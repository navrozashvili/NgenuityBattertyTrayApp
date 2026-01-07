using Google.FlatBuffers;

namespace CommunicationHub;

public struct DeviceInformation : IFlatbufferObject
{
    private Table __p;

    public ByteBuffer ByteBuffer => __p.bb;

    public static void ValidateVersion() => FlatBufferConstants.FLATBUFFERS_25_2_10();

    public static DeviceInformation GetRootAsDeviceInformation(ByteBuffer _bb) =>
        GetRootAsDeviceInformation(_bb, new DeviceInformation());

    public static DeviceInformation GetRootAsDeviceInformation(ByteBuffer _bb, DeviceInformation obj) =>
        obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb);

    public void __init(int _i, ByteBuffer _bb) => __p = new Table(_i, _bb);

    public DeviceInformation __assign(int _i, ByteBuffer _bb)
    {
        __init(_i, _bb);
        return this;
    }

    public string DeviceId
    {
        get
        {
            var o = __p.__offset(4);
            return o == 0 ? string.Empty : __p.__string(o + __p.bb_pos);
        }
    }

    public ProductCategory Category
    {
        get
        {
            var o = __p.__offset(10);
            return o == 0 ? ProductCategory.Unknown : (ProductCategory)__p.bb.Get(o + __p.bb_pos);
        }
    }

    public bool IsWirelessProduct
    {
        get
        {
            var o = __p.__offset(12);
            return o != 0 && __p.bb.Get(o + __p.bb_pos) > 0;
        }
    }

    public Products Product
    {
        get
        {
            var o = __p.__offset(52);
            return o == 0 ? Products.Unknown : (Products)__p.bb.Get(o + __p.bb_pos);
        }
    }

    public bool IsDongle
    {
        get
        {
            var o = __p.__offset(54);
            return o != 0 && __p.bb.Get(o + __p.bb_pos) > 0;
        }
    }

    public bool IsUpdatedByFirmware
    {
        get
        {
            var o = __p.__offset(56);
            return o != 0 && __p.bb.Get(o + __p.bb_pos) > 0;
        }
    }

    public static void StartDeviceInformation(FlatBufferBuilder builder) => builder.StartTable(27);

    public static Offset<DeviceInformation> EndDeviceInformation(FlatBufferBuilder builder) => new(builder.EndTable());
}


