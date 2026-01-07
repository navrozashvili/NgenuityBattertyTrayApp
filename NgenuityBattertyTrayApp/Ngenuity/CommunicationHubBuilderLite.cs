using CommunicationHub;
using Google.FlatBuffers;
using Message = CommunicationHub.Message;

namespace NgenuityBattertyTrayApp.Ngenuity;

internal static class CommunicationHubBuilderLite
{
    public static byte[] CreateDeviceListBuffers()
    {
        var builder = new FlatBufferBuilder(256);
        DeviceList.StartDeviceList(builder);
        var list = DeviceList.EndDeviceList(builder);

        AddRequest.StartAddRequest(builder);
        AddRequest.AddRequestData(builder, list.Value);
        AddRequest.AddRequestDataType(builder, RequestData.DeviceList);
        var req = AddRequest.EndAddRequest(builder);

        Message.StartMessage(builder);
        Message.AddAction(builder, req.Value);
        Message.AddActionType(builder, CommunicationHub.Action.AddRequest);
        Message.FinishMessageBuffer(builder, Message.EndMessage(builder));
        return Message.GetRootAsMessage(builder.DataBuffer).ByteBuffer.ToSizedArray();
    }

    public static byte[] CreateDeviceInfoBuffers(ulong baseId)
    {
        var builder = new FlatBufferBuilder(256);
        DeviceInformation.StartDeviceInformation(builder);
        var info = DeviceInformation.EndDeviceInformation(builder);

        AddCommand.StartAddCommand(builder);
        AddCommand.AddCommandDataType(builder, CommandData.DeviceInformation);
        AddCommand.AddCommandData(builder, info.Value);
        var cmd = AddCommand.EndAddCommand(builder);

        Message.StartMessage(builder);
        Message.AddBaseId(builder, baseId);
        Message.AddAction(builder, cmd.Value);
        Message.AddActionType(builder, CommunicationHub.Action.AddCommand);
        Message.FinishMessageBuffer(builder, Message.EndMessage(builder));
        return Message.GetRootAsMessage(builder.DataBuffer).ByteBuffer.ToSizedArray();
    }

    public static byte[] CreateWirelessRFBatteryInformation(ulong baseId)
    {
        var builder = new FlatBufferBuilder(256);
        WirelessRFBatteryInformation.StartWirelessRFBatteryInformation(builder);
        var info = WirelessRFBatteryInformation.EndWirelessRFBatteryInformation(builder);

        AddCommand.StartAddCommand(builder);
        AddCommand.AddCommandDataType(builder, CommandData.WirelessRFBatteryInformation);
        AddCommand.AddCommandData(builder, info.Value);
        var cmd = AddCommand.EndAddCommand(builder);

        Message.StartMessage(builder);
        Message.AddBaseId(builder, baseId);
        Message.AddAction(builder, cmd.Value);
        Message.AddActionType(builder, CommunicationHub.Action.AddCommand);
        Message.FinishMessageBuffer(builder, Message.EndMessage(builder));
        return Message.GetRootAsMessage(builder.DataBuffer).ByteBuffer.ToSizedArray();
    }

    public static byte[] CreateWirelessRFConnectionStatus(ulong baseId)
    {
        var builder = new FlatBufferBuilder(256);
        WirelessRFConnectionStatus.StartWirelessRFConnectionStatus(builder);
        var info = WirelessRFConnectionStatus.EndWirelessRFConnectionStatus(builder);

        AddCommand.StartAddCommand(builder);
        AddCommand.AddCommandDataType(builder, CommandData.WirelessRFConnectionStatus);
        AddCommand.AddCommandData(builder, info.Value);
        var cmd = AddCommand.EndAddCommand(builder);

        Message.StartMessage(builder);
        Message.AddBaseId(builder, baseId);
        Message.AddAction(builder, cmd.Value);
        Message.AddActionType(builder, CommunicationHub.Action.AddCommand);
        Message.FinishMessageBuffer(builder, Message.EndMessage(builder));
        return Message.GetRootAsMessage(builder.DataBuffer).ByteBuffer.ToSizedArray();
    }
}


