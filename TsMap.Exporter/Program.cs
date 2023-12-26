using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsMap.Exporter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            new Eto.Forms.Application();
            var mapper = new TsMapper(@"C:\Program Files (x86)\Steam\steamapps\common\Euro Truck Simulator 2", new List<Mod>());
            mapper.Parse();
            var exporter = new Mvt.Exporter(mapper);
            exporter.ExportMap(@"C:\Users\edog\Desktop\test");
        }
    }
}
