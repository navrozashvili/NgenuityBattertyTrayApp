using CommunicationHub;
using NetMQ;
using NgenuityBattertyTrayApp.Logging;
using NgenuityBattertyTrayApp.Ngenuity;
using NgenuityBattertyTrayApp.Settings;
using NgenuityBattertyTrayApp.Startup;
using NgenuityBattertyTrayApp.Ui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ChMessage = CommunicationHub.Message;

namespace NgenuityBattertyTrayApp;

internal sealed class TrayAppContext : ApplicationContext
{
    private const string AppName = "NgenuityBatteryTray";

    private readonly AppLogger _log;
    private readonly AppSettings _settings;
    private readonly SynchronizationContext _ui;

    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _devicesRoot;
    private readonly ToolStripMenuItem _startupItem;

    private readonly System.Windows.Forms.Timer _pollTimer;
    private NgenuityHubClient? _hub;

    private readonly Dictionary<ulong, int?> _batteryPercent = new();
    private readonly Dictionary<ulong, ChargeStatus?> _chargeStatus = new();
    private readonly Dictionary<ulong, bool?> _rfConnected = new();
    private readonly Dictionary<ulong, int> _pollNoChargingStreak = new();

    private List<DeviceEntry> _devices = [];
    private ulong? _selectedBaseId;

    public TrayAppContext()
    {
        AppPaths.EnsureCreated();

        var logPath = Path.Combine(AppPaths.LogsDir, $"app-{DateTime.Now:yyyyMMdd}.log");
        _log = new AppLogger(logPath);
        _settings = AppSettings.LoadOrDefault(AppPaths.SettingsPath);
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(_ui);

        _menu = new ContextMenuStrip();
        _devicesRoot = new ToolStripMenuItem("Device");
        _startupItem = new ToolStripMenuItem("Start with Windows");

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = _menu,
            Text = "Ngenuity Battery Tray"
        };

        BuildStaticMenu();

        _pollTimer = new System.Windows.Forms.Timer { Interval = Math.Max(5, _settings.PollIntervalSeconds) * 1000 };
        _pollTimer.Tick += (_, _) => PollSelectedBattery();
        _pollTimer.Start();

        TryConnectHub();
        RefreshDevices();
        PollSelectedBattery();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();

            _hub?.Dispose();
            _hub = null;

            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();

            _menu.Dispose();
            BatteryTrayIconRenderer.DisposeCache();
            _log.Dispose();
        }
        base.Dispose(disposing);
    }

    private void BuildStaticMenu()
    {
        _menu.Items.Clear();
        _menu.Items.Add(_devicesRoot);
        _menu.Items.Add(new ToolStripSeparator());

        _startupItem.Checked = StartupManager.IsEnabled(AppName);
        _startupItem.CheckOnClick = false;
        _startupItem.Click += (_, _) =>
        {
            try
            {
                var enabled = StartupManager.IsEnabled(AppName);
                if (enabled)
                {
                    StartupManager.Disable(AppName);
                    _startupItem.Checked = false;
                    _settings.StartWithWindows = false;
                }
                else
                {
                    StartupManager.Enable(AppName, Application.ExecutablePath);
                    _startupItem.Checked = true;
                    _settings.StartWithWindows = true;
                }
                _settings.Save(AppPaths.SettingsPath);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to toggle startup option");
            }
        };
        _menu.Items.Add(_startupItem);

        var refreshItem = new ToolStripMenuItem("Refresh devices");
        refreshItem.Click += (_, _) => RefreshDevices();
        _menu.Items.Add(refreshItem);

        var openLogsItem = new ToolStripMenuItem("Open log folder");
        openLogsItem.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo("explorer.exe", AppPaths.LogsDir) { UseShellExecute = true }); }
            catch { /* ignore */ }
        };
        _menu.Items.Add(openLogsItem);

        _menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitThread();
        _menu.Items.Add(exitItem);
    }

    private void TryConnectHub()
    {
        try
        {
            _hub = new NgenuityHubClient(_log);
            _hub.NotificationReceived += OnHubNotification;
            _hub.Connect();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to connect to Ngenuity communication hub (is NGENUITY running?)");
            _hub?.Dispose();
            _hub = null;
        }
    }

    private void RefreshDevices()
    {
        try
        {
            if (_hub == null)
                TryConnectHub();
            if (_hub == null)
            {
                _devices = [];
                _selectedBaseId = null;
                RebuildDeviceMenu();
                UpdateTrayDisplay();
                return;
            }

            var baseIds = _hub.GetDeviceList();
            var devices = new List<DeviceEntry>();
            foreach (var baseId in baseIds)
            {
                var info = _hub.GetDeviceInformation(baseId);
                if (info is null)
                    continue;

                // Only show wireless devices. (Battery info is under wireless RF.)
                if (!info.Value.IsWirelessProduct)
                    continue;

                // Mirror NGENUITY's display rule for headsets.
                if (info.Value.Category == ProductCategory.Headset && !info.Value.IsDongle)
                    continue;

                var name = $"{info.Value.Product} ({info.Value.DeviceId})";
                devices.Add(new DeviceEntry(baseId, name, info.Value.Product, info.Value.DeviceId));

                // Prime initial values
                _rfConnected[baseId] = _hub.GetWirelessRFConnectionStatus(baseId);
                var battery = _hub.GetWirelessRFBatteryInformation(baseId);
                if (battery.HasValue)
                {
                    _batteryPercent[baseId] = battery.Value.BatteryLevel;
                    _chargeStatus[baseId] = battery.Value.ChargeStatus;
                }
            }

            _devices = devices.OrderBy(d => d.Name).ToList();

            var desired = _settings.SelectedBaseId;
            if (desired.HasValue && _devices.Any(d => d.BaseId == desired.Value))
                _selectedBaseId = desired.Value;
            else
                _selectedBaseId = _devices.FirstOrDefault().BaseId;

            _settings.SelectedBaseId = _selectedBaseId;
            _settings.Save(AppPaths.SettingsPath);

            RebuildDeviceMenu();
            UpdateTrayDisplay();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to refresh devices");
        }
    }

    private void RebuildDeviceMenu()
    {
        _devicesRoot.DropDownItems.Clear();

        if (_devices.Count == 0)
        {
            var none = new ToolStripMenuItem("(no wireless devices found)") { Enabled = false };
            _devicesRoot.DropDownItems.Add(none);
            return;
        }

        foreach (var d in _devices)
        {
            var item = new ToolStripMenuItem(d.Name)
            {
                Checked = _selectedBaseId.HasValue && _selectedBaseId.Value == d.BaseId
            };
            item.Click += (_, _) =>
            {
                _selectedBaseId = d.BaseId;
                _settings.SelectedBaseId = _selectedBaseId;
                _settings.Save(AppPaths.SettingsPath);
                RebuildDeviceMenu();
                PollSelectedBattery();
            };
            _devicesRoot.DropDownItems.Add(item);
        }
    }

    private void OnHubNotification(Notification notification, ChMessage msg)
    {
        // Marshal to UI thread (NotifyIcon/menu are UI-thread-affine).
        _ui.Post(_ => HandleHubNotification(notification, msg), null);
    }

    private void HandleHubNotification(Notification notification, ChMessage msg)
    {
        try
        {
            switch (notification)
            {
                case Notification.DeviceAdded:
                case Notification.DeviceRemoved:
                    RefreshDevices();
                    break;

                case Notification.WirelessRFDongleStatusNotification:
                {
                    var n = msg.ActionAsIncomingNotification().NotificationAsWirelessRFDongleStatusNotification();
                    _rfConnected[msg.BaseId] = n.Connected;
                    if (_selectedBaseId == msg.BaseId)
                        UpdateTrayDisplay();
                    break;
                }

                case Notification.BatteryChargingNotification:
                {
                    var n = msg.ActionAsIncomingNotification().NotificationAsBatteryChargingNotification();
                    _chargeStatus[msg.BaseId] = n.ChargeStatus;
                    _pollNoChargingStreak[msg.BaseId] = 0;
                    if (_selectedBaseId == msg.BaseId)
                        UpdateTrayDisplay();
                    break;
                }

                case Notification.BatteryLevelIndicatorChangedNotification:
                {
                    var n = msg.ActionAsIncomingNotification().NotificationAsBatteryLevelIndicatorChangedNotification();
                    _batteryPercent[msg.BaseId] = n.BatteryLevel;
                    if (_selectedBaseId == msg.BaseId)
                        UpdateTrayDisplay();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed handling notification {notification}");
        }
    }

    private void PollSelectedBattery()
    {
        try
        {
            if (_hub == null)
                TryConnectHub();
            if (_hub == null)
            {
                UpdateTrayDisplay();
                return;
            }

            if (!_selectedBaseId.HasValue)
            {
                UpdateTrayDisplay();
                return;
            }

            var baseId = _selectedBaseId.Value;
            _rfConnected[baseId] = _hub.GetWirelessRFConnectionStatus(baseId);

            var battery = _hub.GetWirelessRFBatteryInformation(baseId);
            if (battery.HasValue)
            {
                _batteryPercent[baseId] = battery.Value.BatteryLevel;

                // ChargeStatus is best sourced from BatteryChargingNotification.
                // Some devices appear to report "NoCharging" in the polled battery info even while charging.
                // So: only "upgrade" from poll, and only "downgrade" after repeated consistent polls.
                var polledCs = battery.Value.ChargeStatus;
                _chargeStatus.TryGetValue(baseId, out var existingCs);

                if (existingCs is null)
                {
                    _chargeStatus[baseId] = polledCs;
                    _pollNoChargingStreak[baseId] = 0;
                }
                else if ((existingCs == ChargeStatus.NoCharging || existingCs == ChargeStatus.Unknown) &&
                         (polledCs == ChargeStatus.Charging || polledCs == ChargeStatus.FullyCharged))
                {
                    _chargeStatus[baseId] = polledCs;
                    _pollNoChargingStreak[baseId] = 0;
                }
                else if ((existingCs == ChargeStatus.Charging || existingCs == ChargeStatus.FullyCharged) &&
                         polledCs == ChargeStatus.NoCharging)
                {
                    var streak = _pollNoChargingStreak.TryGetValue(baseId, out var s) ? s : 0;
                    streak++;
                    _pollNoChargingStreak[baseId] = streak;

                    // Only downgrade after a few consecutive polls, in case polling is briefly wrong/noisy.
                    if (streak >= 3)
                    {
                        _chargeStatus[baseId] = ChargeStatus.NoCharging;
                        _pollNoChargingStreak[baseId] = 0;
                    }
                }
                else
                {
                    _pollNoChargingStreak[baseId] = 0;
                }
            }

            UpdateTrayDisplay();
        }
        catch (Exception ex) when (ex is TimeoutException or NetMQ.FiniteStateMachineException)
        {
            // Transient IPC hiccups can happen if NGENUITY is busy/restarting.
            // These are recoverable; keep the tray UI responsive and avoid scary error spam.
            _log.Warn(ex, "Polling battery temporarily failed");
            UpdateTrayDisplay();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Polling battery failed");
            UpdateTrayDisplay();
        }
    }

    private void UpdateTrayDisplay()
    {
        var selected = _devices.FirstOrDefault(d => _selectedBaseId.HasValue && d.BaseId == _selectedBaseId.Value);
        var baseId = _selectedBaseId;

        int? pct = null;
        ChargeStatus? cs = null;
        bool? connected = null;

        if (baseId.HasValue)
        {
            _batteryPercent.TryGetValue(baseId.Value, out pct);
            _chargeStatus.TryGetValue(baseId.Value, out cs);
            _rfConnected.TryGetValue(baseId.Value, out connected);
        }

        var isCharging = cs is ChargeStatus.Charging or ChargeStatus.FullyCharged;
        if (connected is false)
            pct = null;

        _notifyIcon.Icon = BatteryTrayIconRenderer.GetIcon(pct, isCharging);

        var shortName = selected.Name ?? "Ngenuity device";
        var chargeSuffix = cs switch
        {
            ChargeStatus.Charging => " (Charging)",
            ChargeStatus.FullyCharged => " (Full)",
            ChargeStatus.ChargerError => " (Charger error)",
            _ => ""
        };
        var status =
            connected is false ? "Disconnected" :
            pct is >= 0 and <= 100 ? $"{pct}%{chargeSuffix}" :
            "N/A";

        // NotifyIcon.Text is limited (~63 chars).
        var text = $"{shortName}: {status}";
        _notifyIcon.Text = text.Length > 63 ? text.Substring(0, 63) : text;
    }

    private readonly record struct DeviceEntry(ulong BaseId, string Name, Products Model, string DeviceId);
}


