using MessagePack;
using System.IO;
using System.IO.Compression;
using TsMap.Exporter.Data;
using TsMap.Exporter.Mvt;
using TsMap.Exporter.Overlays;
using TsMap.Exporter.Routing;

namespace TsMap.Exporter
{
    public abstract class BaseExporter
    {
        public readonly TsMapper Mapper;

        protected BaseExporter(TsMapper mapper)
        {
            Mapper = mapper;
        }

        public virtual void Prepare() { }
    }

    public abstract class MsgPackExporter : BaseExporter
    {
        protected static MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        protected MsgPackExporter(TsMapper mapper) : base(mapper)
        {
        }

        protected byte[] GetMsgPack(object o)
        {
            return MessagePackSerializer.Serialize(o, Options);
        }
    }
}
