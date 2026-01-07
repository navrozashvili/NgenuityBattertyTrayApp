using CommunicationHub;
using Google.FlatBuffers;
using NetMQ;
using NetMQ.Sockets;
using NgenuityBattertyTrayApp.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChMessage = CommunicationHub.Message;

namespace NgenuityBattertyTrayApp.Ngenuity;

internal sealed class NgenuityHubClient : IDisposable
{
    private static readonly byte[] HxAck = [119, 1, 36];
    private const int ServerPortStart = 6890;
    private const int ServerPortEndExclusive = 6900;
    private const int SubscriberPortStart = 7890;
    private const int SubscriberPortEndExclusive = 7900;

    private readonly AppLogger _log;
    private RequestSocket? _req;
    private SubscriberSocket? _sub;
    private CancellationTokenSource? _cts;
    private readonly object _reqLock = new();

    public event Action<Notification, ChMessage>? NotificationReceived;

    public NgenuityHubClient(AppLogger log)
    {
        _log = log;
    }

    public void Connect()
    {
        if (_req != null)
            return;

        _req = ConnectRequestSocket();
        _sub = ConnectSubscriberSocket();

        _cts = new CancellationTokenSource();
        StartSubscriberLoop(_cts.Token);

        // Subscribe only to what we need.
        Subscribe(Notification.DeviceAdded);
        Subscribe(Notification.DeviceRemoved);
        Subscribe(Notification.WirelessRFDongleStatusNotification);
        Subscribe(Notification.BatteryChargingNotification);
        Subscribe(Notification.BatteryLevelIndicatorChangedNotification);
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        _cts?.Dispose();
        _cts = null;

        _sub?.Dispose();
        _sub = null;

        _req?.Dispose();
        _req = null;
    }

    public IReadOnlyList<ulong> GetDeviceList()
    {
        var bytes = Send(CommunicationHubBuilderLite.CreateDeviceListBuffers());
        var msg = ChMessage.GetRootAsMessage(new ByteBuffer(bytes));
        var list = msg.ActionAsAddRequest().RequestDataAsDeviceList().GetBaseIdsArray();
        return list?.ToList() ?? [];
    }

    public DeviceInformation? GetDeviceInformation(ulong baseId)
    {
        var bytes = Send(CommunicationHubBuilderLite.CreateDeviceInfoBuffers(baseId));
        var msg = ChMessage.GetRootAsMessage(new ByteBuffer(bytes));
        if (!string.IsNullOrEmpty(msg.ErrMessage))
            return null;
        return msg.ActionAsAddCommand().CommandDataAsDeviceInformation();
    }

    public WirelessRFBatteryInformation? GetWirelessRFBatteryInformation(ulong baseId)
    {
        var bytes = Send(CommunicationHubBuilderLite.CreateWirelessRFBatteryInformation(baseId));
        var msg = ChMessage.GetRootAsMessage(new ByteBuffer(bytes));
        if (!string.IsNullOrEmpty(msg.ErrMessage))
            return null;
        return msg.ActionAsAddCommand().CommandDataAsWirelessRFBatteryInformation();
    }

    public bool? GetWirelessRFConnectionStatus(ulong baseId)
    {
        var bytes = Send(CommunicationHubBuilderLite.CreateWirelessRFConnectionStatus(baseId));
        var msg = ChMessage.GetRootAsMessage(new ByteBuffer(bytes));
        if (!string.IsNullOrEmpty(msg.ErrMessage))
            return null;
        return msg.ActionAsAddCommand().CommandDataAsWirelessRFConnectionStatus().Connected;
    }

    private byte[] Send(byte[] request)
    {
        if (_req == null)
            throw new InvalidOperationException("Ngenuity IPC is not connected.");

        lock (_reqLock)
        {
            _req.SendFrame(request);
            if (_req.TryReceiveFrameBytes(TimeSpan.FromMilliseconds(750), out var resp))
                return resp;
        }

        throw new TimeoutException("Timed out waiting for response from ngenuity3-srv-communication-hub.");
    }

    private RequestSocket ConnectRequestSocket()
    {
        for (var port = ServerPortStart; port < ServerPortEndExclusive; port++)
        {
            var socket = new RequestSocket();
            var addr = $"tcp://localhost:{port}";
            try
            {
                socket.Connect(addr);
                socket.SendFrame(HxAck);
                if (socket.TryReceiveFrameBytes(TimeSpan.FromMilliseconds(800), out _))
                {
                    _log.Info($"IPC request connected: {addr}");
                    return socket;
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"IPC request connect failed on {addr}: {ex.Message}");
            }

            try { socket.Dispose(); } catch { /* ignore */ }
        }

        throw new Exception($"No ngenuity communication hub server found on ports {ServerPortStart}-{ServerPortEndExclusive - 1}.");
    }

    private SubscriberSocket ConnectSubscriberSocket()
    {
        for (var port = SubscriberPortStart; port < SubscriberPortEndExclusive; port++)
        {
            var socket = new SubscriberSocket();
            var addr = $"tcp://localhost:{port}";
            try
            {
                socket.Connect(addr);
                _log.Info($"IPC subscriber connected: {addr}");
                return socket;
            }
            catch (Exception ex)
            {
                _log.Warn($"IPC subscriber connect failed on {addr}: {ex.Message}");
            }

            try { socket.Dispose(); } catch { /* ignore */ }
        }

        throw new Exception($"No ngenuity communication hub publisher found on ports {SubscriberPortStart}-{SubscriberPortEndExclusive - 1}.");
    }

    private void Subscribe(Notification notification)
    {
        if (_sub == null) return;
        var topic = new byte[] { 13, (byte)notification, 0 };
        _sub.Subscribe(topic);
    }

    private void StartSubscriberLoop(CancellationToken token)
    {
        if (_sub == null) return;

        _ = Task.Run(() =>
        {
            // We do prefix matching ourselves because the publisher packs topic+payload into one frame.
            var topics = new[]
            {
                new byte[] { 13, (byte)Notification.DeviceAdded, 0 },
                new byte[] { 13, (byte)Notification.DeviceRemoved, 0 },
                new byte[] { 13, (byte)Notification.WirelessRFDongleStatusNotification, 0 },
                new byte[] { 13, (byte)Notification.BatteryChargingNotification, 0 },
                new byte[] { 13, (byte)Notification.BatteryLevelIndicatorChangedNotification, 0 },
            };

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!_sub.TryReceiveFrameBytes(TimeSpan.FromMilliseconds(50), out var frame) || frame is null)
                        continue;

                    foreach (var t in topics)
                    {
                        if (frame.Length <= t.Length)
                            continue;

                        if (!frame.AsSpan(0, t.Length).SequenceEqual(t))
                            continue;

                        var payload = frame.AsSpan(t.Length).ToArray();
                        var msg = ChMessage.GetRootAsMessage(new ByteBuffer(payload));
                        NotificationReceived?.Invoke((Notification)t[1], msg);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                        _log.Error(ex, "IPC subscriber loop error");
                }
            }
        }, token);
    }
}


