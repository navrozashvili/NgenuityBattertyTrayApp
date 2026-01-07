using Google.FlatBuffers;

namespace CommunicationHub;

public struct BatteryLevelIndicatorChangedNotification : IFlatbufferObject
{
    private Table __p;

    public ByteBuffer ByteBuffer => __p.bb;

    public static void ValidateVersion() => FlatBufferConstants.FLATBUFFERS_25_2_10();

    public static BatteryLevelIndicatorChangedNotification GetRootAsBatteryLevelIndicatorChangedNotification(ByteBuffer _bb) =>
        GetRootAsBatteryLevelIndicatorChangedNotification(_bb, new BatteryLevelIndicatorChangedNotification());

    public static BatteryLevelIndicatorChangedNotification GetRootAsBatteryLevelIndicatorChangedNotification(ByteBuffer _bb, BatteryLevelIndicatorChangedNotification obj) =>
        obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb);

    public void __init(int _i, ByteBuffer _bb) => __p = new Table(_i, _bb);

    public BatteryLevelIndicatorChangedNotification __assign(int _i, ByteBuffer _bb)
    {
        __init(_i, _bb);
        return this;
    }

    public byte BatteryLevel
    {
        get
        {
            var o = __p.__offset(6);
            return o == 0 ? (byte)0 : __p.bb.Get(o + __p.bb_pos);
        }
    }
}


