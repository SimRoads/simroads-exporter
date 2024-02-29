using System.IO;
using System.IO.Compression;
using System.Linq;

namespace TsMap.Exporter.Routing
{
    public class RoutingExporter(TsMapper mapper) : MsgPackExporter(mapper)
    {
        public override void Export(ZipArchive zipArchive)
        {
            var nodes = RoutingNode.GetNetwork(Mapper);

            WriteMsgPack(zipArchive, Path.Join("json", "routing", "nodes.msgpack"),
                nodes.Values.Select(x => x.Serialize()).ToList());
            WriteMsgPack(zipArchive, Path.Join("json", "routing", "links.msgpack"),
                nodes.Values.SelectMany(x => x.GetLinks()).Select(x => x.Serialize()).ToArray());
        }
    }
}