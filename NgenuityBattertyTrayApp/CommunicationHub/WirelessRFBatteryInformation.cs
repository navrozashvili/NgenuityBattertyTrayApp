using Google.FlatBuffers;

namespace CommunicationHub;

public struct WirelessRFBatteryInformation : IFlatbufferObject
{
    private Table __p;

    public ByteBuffer ByteBuffer => __p.bb;

    public static void ValidateVersion() => FlatBufferConstants.FLATBUFFERS_25_2_10();

    public static WirelessRFBatteryInformation GetRootAsWirelessRFBatteryInformation(ByteBuffer _bb) =>
        GetRootAsWirelessRFBatteryInformation(_bb, new WirelessRFBatteryInformation());

    public static WirelessRFBatteryInformation GetRootAsWirelessRFBatteryInformation(ByteBuffer _bb, WirelessRFBatteryInformation obj) =>
        obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb);

    public void __init(int _i, ByteBuffer _bb) => __p = new Table(_i, _bb);

    public WirelessRFBatteryInformation __assign(int _i, ByteBuffer _bb)
    {
        __init(_i, _bb);
        return this;
    }

    public byte BatteryLevel
    {
        get
        {
            var o = __p.__offset(4);
            return o == 0 ? (byte)0 : __p.bb.Get(o + __p.bb_pos);
        }
    }

    public ChargeStatus ChargeStatus
    {
        get
        {
            var o = __p.__offset(6);
            return o == 0 ? ChargeStatus.NoCharging : (ChargeStatus)__p.bb.Get(o + __p.bb_pos);
        }
    }

    public static void StartWirelessRFBatteryInformation(FlatBufferBuilder builder) => builder.StartTable(7);

    public static Offset<WirelessRFBatteryInformation> EndWirelessRFBatteryInformation(FlatBufferBuilder builder) => new(builder.EndTable());
}


