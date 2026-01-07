using Google.FlatBuffers;

namespace CommunicationHub;

public struct WirelessRFDongleStatusNotification : IFlatbufferObject
{
    private Table __p;

    public ByteBuffer ByteBuffer => __p.bb;

    public static void ValidateVersion() => FlatBufferConstants.FLATBUFFERS_25_2_10();

    public static WirelessRFDongleStatusNotification GetRootAsWirelessRFDongleStatusNotification(ByteBuffer _bb) =>
        GetRootAsWirelessRFDongleStatusNotification(_bb, new WirelessRFDongleStatusNotification());

    public static WirelessRFDongleStatusNotification GetRootAsWirelessRFDongleStatusNotification(ByteBuffer _bb, WirelessRFDongleStatusNotification obj) =>
        obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb);

    public void __init(int _i, ByteBuffer _bb) => __p = new Table(_i, _bb);

    public WirelessRFDongleStatusNotification __assign(int _i, ByteBuffer _bb)
    {
        __init(_i, _bb);
        return this;
    }

    public bool Connected
    {
        get
        {
            var o = __p.__offset(4);
            return o != 0 && __p.bb.Get(o + __p.bb_pos) > 0;
        }
    }
}


