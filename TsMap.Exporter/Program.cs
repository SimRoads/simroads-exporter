using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TsMap.Exporter.Data;
using TsMap.Exporter.Mvt;
using TsMap.Exporter.Overlays;

namespace TsMap.Exporter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            new Eto.Forms.Application();
            var mapper = new TsMapper(@"C:\Program Files (x86)\Steam\steamapps\common\Euro Truck Simulator 2", new List<Mod>());
            mapper.Parse();
            using (FileStream zipToOpen = new FileStream(@"C:\Users\edog\Desktop\test\test.zip", FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    /*var exporter = new MvtExporter(mapper);
                    exporter.Export(archive);*/

                    /*var overlayExporter = new OverlayExporter(mapper);
                    overlayExporter.Export(archive);*/

                    var dataExporter = new DataExporter(mapper);
                     dataExporter.Export(archive);

                }
            }
        }
    }
}
