using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace TsMap.Exporter.Routing
{
    public class RoutingExporter : BaseExporter
    {
        private static MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);

        public RoutingExporter(TsMapper mapper) : base(mapper)
        {
        }

        public override void Export(ZipArchive zipArchive)
        {
            var nodes =  RoutingNode.GetNetwork(mapper);

            var zipArchiveEntry = zipArchive.CreateEntry(Path.Join("routing", "nodes.msgpack"), CompressionLevel.Fastest);
            using (var stream = zipArchiveEntry.Open())
            {
                stream.Write(MessagePackSerializer.Serialize(nodes.Values.Select(x => x.Serialize()), Options));
            }

            zipArchiveEntry = zipArchive.CreateEntry(Path.Join("routing", "links.msgpack"), CompressionLevel.Fastest);
            using (var stream = zipArchiveEntry.Open())
            {
                stream.Write(MessagePackSerializer.Serialize(nodes.Values.SelectMany(x => x.GetLinks()).Select(x => x.Serialize()).ToArray(), Options));
            }
        }
    }
}
