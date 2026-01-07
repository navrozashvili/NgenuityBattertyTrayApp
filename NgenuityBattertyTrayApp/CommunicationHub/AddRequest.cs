using Google.FlatBuffers;

namespace CommunicationHub;

public struct AddRequest : IFlatbufferObject
{
    private Table __p;

    public ByteBuffer ByteBuffer => __p.bb;

    public static void ValidateVersion() => FlatBufferConstants.FLATBUFFERS_25_2_10();

    public static AddRequest GetRootAsAddRequest(ByteBuffer _bb) => GetRootAsAddRequest(_bb, new AddRequest());

    public static AddRequest GetRootAsAddRequest(ByteBuffer _bb, AddRequest obj) =>
        obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb);

    public void __init(int _i, ByteBuffer _bb) => __p = new Table(_i, _bb);

    public AddRequest __assign(int _i, ByteBuffer _bb)
    {
        __init(_i, _bb);
        return this;
    }

    public CommunicationHub.RequestData RequestDataType
    {
        get
        {
            var o = __p.__offset(4);
            return o == 0 ? CommunicationHub.RequestData.NONE : (CommunicationHub.RequestData)__p.bb.Get(o + __p.bb_pos);
        }
    }

    public TTable? RequestData<TTable>() where TTable : struct, IFlatbufferObject
    {
        var o = __p.__offset(6);
        return o == 0 ? null : __p.__union<TTable>(o + __p.bb_pos);
    }

    public DeviceList RequestDataAsDeviceList() => RequestData<DeviceList>().Value;

    public static void StartAddRequest(FlatBufferBuilder builder) => builder.StartTable(2);

    public static void AddRequestDataType(FlatBufferBuilder builder, CommunicationHub.RequestData requestDataType) =>
        builder.AddByte(0, (byte)requestDataType, 0);

    public static void AddRequestData(FlatBufferBuilder builder, int requestDataOffset) =>
        builder.AddOffset(1, requestDataOffset, 0);

    public static Offset<AddRequest> EndAddRequest(FlatBufferBuilder builder) => new(builder.EndTable());
}


