using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TsMap.Helpers;
using TsMap.Helpers.Logger;

namespace TsMap.Exporter.Data;

public class SearchExporter(DataExporter dataExporter) : MsgPackExporter(dataExporter.Mapper)
{
    public override void Export(ZipArchive zipArchive)
    {
        WriteMsgPack(zipArchive, Path.Join("json", "search.msgpack"),
            dataExporter.Countries.Cast<ExpElement>().Concat(dataExporter.Cities)
                .Concat(dataExporter.OverlaysByPrefab.SelectMany(x => x.Value)).Select(x => x.ExportIndex()).ToArray());
        Logger.Instance.Info($"Exported search index file");
    }
}