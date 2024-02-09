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
        protected readonly TsMapper mapper;

        public BaseExporter(TsMapper mapper)
        {
            this.mapper = mapper;
        }

        public abstract void Export(ZipArchive zipArchive);

        public static void ExportAll(TsMapper mapper, string zipPath)
        {
            using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    BaseExporter exporter = new MvtExporter(mapper);
                    exporter.Export(archive);

                    exporter = new OverlayExporter(mapper);
                    exporter.Export(archive);

                    exporter = new DataExporter(mapper);
                    exporter.Export(archive);

                    exporter = new RoutingExporter(mapper);
                    exporter.Export(archive);
                }
            }
        }
    }

}
