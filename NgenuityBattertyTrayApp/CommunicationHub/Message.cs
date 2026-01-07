using Google.FlatBuffers;
using System;

namespace CommunicationHub;

public struct Message : IFlatbufferObject
{
    private Table __p;

    public ByteBuffer ByteBuffer => __p.bb;

    public static void ValidateVersion() => FlatBufferConstants.FLATBUFFERS_25_2_10();

    public static Message GetRootAsMessage(ByteBuffer _bb) => GetRootAsMessage(_bb, new Message());

    public static Message GetRootAsMessage(ByteBuffer _bb, Message obj) =>
        obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb);

    public void __init(int _i, ByteBuffer _bb) => __p = new Table(_i, _bb);

    public Message __assign(int _i, ByteBuffer _bb)
    {
        __init(_i, _bb);
        return this;
    }

    public ulong BaseId
    {
        get
        {
            var o = __p.__offset(4);
            return o == 0 ? 0UL : __p.bb.GetUlong(o + __p.bb_pos);
        }
    }

    public CommunicationHub.Action ActionType
    {
        get
        {
            var o = __p.__offset(6);
            return o == 0 ? CommunicationHub.Action.NONE : (CommunicationHub.Action)__p.bb.Get(o + __p.bb_pos);
        }
    }

    public TTable? Action<TTable>() where TTable : struct, IFlatbufferObject
    {
        var o = __p.__offset(8);
        return o == 0 ? null : __p.__union<TTable>(o + __p.bb_pos);
    }

    public AddCommand ActionAsAddCommand() => Action<AddCommand>().Value;
    public AddRequest ActionAsAddRequest() => Action<AddRequest>().Value;
    public IncomingNotification ActionAsIncomingNotification() => Action<IncomingNotification>().Value;

    public string? ErrMessage
    {
        get
        {
            var o = __p.__offset(10);
            return o == 0 ? null : __p.__string(o + __p.bb_pos);
        }
    }

    public static void StartMessage(FlatBufferBuilder builder) => builder.StartTable(4);

    public static void AddBaseId(FlatBufferBuilder builder, ulong baseId) => builder.AddUlong(0, baseId, 0UL);

    public static void AddActionType(FlatBufferBuilder builder, CommunicationHub.Action actionType) => builder.AddByte(1, (byte)actionType, 0);

    public static void AddAction(FlatBufferBuilder builder, int actionOffset) => builder.AddOffset(2, actionOffset, 0);

    public static Offset<Message> EndMessage(FlatBufferBuilder builder) => new(builder.EndTable());

    public static void FinishMessageBuffer(FlatBufferBuilder builder, Offset<Message> offset) => builder.Finish(offset.Value);
}


