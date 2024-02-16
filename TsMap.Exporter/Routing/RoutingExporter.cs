using MessagePack;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace TsMap.Exporter.Routing
{
    public class RoutingExporter : BaseExporter
    {
        private Dictionary<ulong, RoutingNode> network;

        public RoutingExporter(TsMapper mapper) : base(mapper)
        {
        }

        public override void Prepare()
        {
            network = RoutingNode.GetNetwork(Mapper);
        }

        public object ExportNodes()
        {
            return network.Values.Select(x => x.Serialize());
        }

        public object ExportLinks()
        {
            return network.Values.SelectMany(x => x.GetLinks()).Select(x => x.Serialize()).ToArray();
        }
    }
}
