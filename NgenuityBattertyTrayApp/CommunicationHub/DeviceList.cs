using Google.FlatBuffers;
using System;

namespace CommunicationHub;

public struct DeviceList : IFlatbufferObject
{
    private Table __p;

    public ByteBuffer ByteBuffer => __p.bb;

    public static void ValidateVersion() => FlatBufferConstants.FLATBUFFERS_25_2_10();

    public static DeviceList GetRootAsDeviceList(ByteBuffer _bb) => GetRootAsDeviceList(_bb, new DeviceList());

    public static DeviceList GetRootAsDeviceList(ByteBuffer _bb, DeviceList obj) =>
        obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb);

    public void __init(int _i, ByteBuffer _bb) => __p = new Table(_i, _bb);

    public DeviceList __assign(int _i, ByteBuffer _bb)
    {
        __init(_i, _bb);
        return this;
    }

    public ulong[] GetBaseIdsArray() => __p.__vector_as_array<ulong>(4);

    public static void StartDeviceList(FlatBufferBuilder builder) => builder.StartTable(1);

    public static Offset<DeviceList> EndDeviceList(FlatBufferBuilder builder) => new(builder.EndTable());
}


