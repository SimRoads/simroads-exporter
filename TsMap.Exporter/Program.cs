using System;
using System.Linq;
using CommandLine;

namespace TsMap.Exporter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ExportSettings>(args).WithParsed(opt => Run(opt));
        }

        static int Run(ExportSettings settings)
        {
            var mods = settings.Mods.Select(x => new Mod(x, true)).ToList();
            var mapper = new TsMapper(settings.GameDir, mods);
            mapper.Parse();

            BaseExporter.ExportAll(mapper, settings);

            return 0;
        }
    }
}