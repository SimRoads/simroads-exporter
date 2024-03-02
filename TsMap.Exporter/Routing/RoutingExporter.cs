using System.IO;
using System.IO.Compression;
using System.Linq;
using TsMap.Helpers.Logger;

namespace TsMap.Exporter.Routing
{
    public class RoutingExporter(TsMapper mapper) : MsgPackExporter(mapper)
    {
        public override void Export(ZipArchive zipArchive)
        {
            var nodes = RoutingNode.GetNetwork(Mapper);

            WriteMsgPack(zipArchive, Path.Join("json", "routing", "nodes.msgpack"),
                nodes.Values.Select(x => x.Serialize()).ToList());
            Logger.Instance.Info($"Exported routing nodes file");
            WriteMsgPack(zipArchive, Path.Join("json", "routing", "links.msgpack"),
                nodes.Values.SelectMany(x => x.GetLinks()).Select(x => x.Serialize()).ToArray());
            Logger.Instance.Info($"Exported routing links file");
        }
    }
}