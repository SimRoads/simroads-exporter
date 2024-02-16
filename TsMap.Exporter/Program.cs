using System;
using System.Linq;

namespace TsMap.Exporter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: TsMap.Exporter <game_dir> <export_file> [mods_paths]");
                return;
            }
            var gameDir = args[0];
            var exportFile = args[1];

            var mods = args.Skip(2).Select(x => new Mod(x)).ToList();
            var mapper = new TsMapper(gameDir, mods);
            mapper.Parse();

            //BaseExporter.ExportAll(mapper, exportFile);
        }
    }

    public static partial class JSBaseExporter { }
}
