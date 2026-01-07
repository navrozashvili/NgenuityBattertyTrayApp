using Google.FlatBuffers;

namespace CommunicationHub;

public struct AddCommand : IFlatbufferObject
{
    private Table __p;

    public ByteBuffer ByteBuffer => __p.bb;

    public static void ValidateVersion() => FlatBufferConstants.FLATBUFFERS_25_2_10();

    public static AddCommand GetRootAsAddCommand(ByteBuffer _bb) => GetRootAsAddCommand(_bb, new AddCommand());

    public static AddCommand GetRootAsAddCommand(ByteBuffer _bb, AddCommand obj) =>
        obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb);

    public void __init(int _i, ByteBuffer _bb) => __p = new Table(_i, _bb);

    public AddCommand __assign(int _i, ByteBuffer _bb)
    {
        __init(_i, _bb);
        return this;
    }

    public CommunicationHub.CommandData CommandDataType
    {
        get
        {
            var o = __p.__offset(4);
            return o == 0 ? CommunicationHub.CommandData.NONE : (CommunicationHub.CommandData)__p.bb.Get(o + __p.bb_pos);
        }
    }

    public TTable? CommandData<TTable>() where TTable : struct, IFlatbufferObject
    {
        var o = __p.__offset(6);
        return o == 0 ? null : __p.__union<TTable>(o + __p.bb_pos);
    }

    public DeviceInformation CommandDataAsDeviceInformation() => CommandData<DeviceInformation>().Value;
    public WirelessRFBatteryInformation CommandDataAsWirelessRFBatteryInformation() => CommandData<WirelessRFBatteryInformation>().Value;
    public WirelessRFConnectionStatus CommandDataAsWirelessRFConnectionStatus() => CommandData<WirelessRFConnectionStatus>().Value;

    public static void StartAddCommand(FlatBufferBuilder builder) => builder.StartTable(2);

    public static void AddCommandDataType(FlatBufferBuilder builder, CommunicationHub.CommandData commandDataType) =>
        builder.AddByte(0, (byte)commandDataType, 0);

    public static void AddCommandData(FlatBufferBuilder builder, int commandDataOffset) =>
        builder.AddOffset(1, commandDataOffset, 0);

    public static Offset<AddCommand> EndAddCommand(FlatBufferBuilder builder) => new(builder.EndTable());
}


