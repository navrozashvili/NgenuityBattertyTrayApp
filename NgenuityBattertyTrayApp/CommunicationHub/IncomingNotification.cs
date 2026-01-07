using Google.FlatBuffers;

namespace CommunicationHub;

public struct IncomingNotification : IFlatbufferObject
{
    private Table __p;

    public ByteBuffer ByteBuffer => __p.bb;

    public static void ValidateVersion() => FlatBufferConstants.FLATBUFFERS_25_2_10();

    public static IncomingNotification GetRootAsIncomingNotification(ByteBuffer _bb) =>
        GetRootAsIncomingNotification(_bb, new IncomingNotification());

    public static IncomingNotification GetRootAsIncomingNotification(ByteBuffer _bb, IncomingNotification obj) =>
        obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb);

    public void __init(int _i, ByteBuffer _bb) => __p = new Table(_i, _bb);

    public IncomingNotification __assign(int _i, ByteBuffer _bb)
    {
        __init(_i, _bb);
        return this;
    }

    public CommunicationHub.Notification NotificationType
    {
        get
        {
            var o = __p.__offset(4);
            return o == 0 ? CommunicationHub.Notification.NONE : (CommunicationHub.Notification)__p.bb.Get(o + __p.bb_pos);
        }
    }

    public TTable? Notification<TTable>() where TTable : struct, IFlatbufferObject
    {
        var o = __p.__offset(6);
        return o == 0 ? null : __p.__union<TTable>(o + __p.bb_pos);
    }

    public BatteryChargingNotification NotificationAsBatteryChargingNotification() => Notification<BatteryChargingNotification>().Value;

    public BatteryLevelIndicatorChangedNotification NotificationAsBatteryLevelIndicatorChangedNotification() =>
        Notification<BatteryLevelIndicatorChangedNotification>().Value;

    public WirelessRFDongleStatusNotification NotificationAsWirelessRFDongleStatusNotification() =>
        Notification<WirelessRFDongleStatusNotification>().Value;
}


