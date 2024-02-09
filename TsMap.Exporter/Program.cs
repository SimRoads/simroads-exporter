using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TsMap.Exporter.Data;
using TsMap.Exporter.Mvt;
using TsMap.Exporter.Overlays;
using TsMap.Exporter.Routing;

namespace TsMap.Exporter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            new Eto.Forms.Application();
            var gameDir = Environment.GetEnvironmentVariable("GAME_DIR");
            var exportFile = Environment.GetEnvironmentVariable("EXPORT_FILE");
            if (gameDir == null)
            {
                Console.WriteLine("GAME_DIR environment variable not set");
                return;
            }
            if (exportFile == null)
            {
                Console.WriteLine("EXPORT_FILE environment variable not set");
                return;
            }

            var mods = Environment.GetEnvironmentVariable("MODS")?.Split(";").Select(x => new Mod(x)).ToList() ?? new();
            var mapper = new TsMapper(gameDir, mods);
            mapper.Parse();

            BaseExporter.ExportAll(mapper, exportFile);
        }
    }
}
