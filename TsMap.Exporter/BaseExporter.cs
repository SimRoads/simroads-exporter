using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using CommandLine;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using MessagePack;
using TsMap.Exporter.Data;
using TsMap.Exporter.Mvt;
using TsMap.Exporter.Overlays;
using TsMap.Exporter.Routing;

namespace TsMap.Exporter
{
    public class ExportSettings
    {
        [Option('g', "game-dir", Required = true, HelpText = "Sets game directory.")]
        public string GameDir { get; set; }

        [Option('m', "mods", Required = false, HelpText = "Comma-separated mods list", Separator = ',')]
        public IEnumerable<string> Mods { get; set; } = [];

        [Option('p', "zip-path", Required = true, HelpText = "Sets export zip path.")]
        public string ZipPath { get; set; }

        [Option('n', "map-name", Required = true, HelpText = "Sets map name.")]
        public string MapName { get; set; }

        [Option('z', "zoom-limit", Required = false, HelpText = "Sets mvt zoom limit.")]
        public uint ZoomLimit { get; set; } = 7;

        public ExportSettings()
        {
        }

        public ExportSettings(string gameDir, string zipPath, string mapName, string[] mods = null, uint zoomLimit = 7)
        {
            GameDir = gameDir;
            ZipPath = zipPath;
            MapName = mapName;
            ZoomLimit = zoomLimit;
            Mods = mods ?? Mods;
        }
    }

    public abstract class BaseExporter(TsMapper mapper)
    {
        protected readonly TsMapper Mapper = mapper;

        public abstract void Export(ZipArchive zipArchive);

        public static void ExportAll(TsMapper mapper, ExportSettings settings)
        {
            using FileStream zipToOpen = new FileStream(settings.ZipPath, FileMode.Create);
            using ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update);

            BaseExporter exporter = new MvtExporter(mapper, settings.ZoomLimit);
            exporter.Export(archive);

            exporter = new OverlayExporter(mapper);
            exporter.Export(archive);

            exporter = new DataExporter(mapper, settings);
            exporter.Export(archive);

            exporter = new RoutingExporter(mapper);
            exporter.Export(archive);
        }
    }

    public abstract class MsgPackExporter(TsMapper mapper) : BaseExporter(mapper)
    {
        private static readonly MessagePackSerializerOptions MsgPackOptions = MessagePackSerializerOptions.Standard;

        protected void WriteMsgPack(ZipArchive archive, string path, object obj)
        {
            using var ms = new MemoryStream(MessagePackSerializer.Serialize(obj, MsgPackOptions));
            ZipArchiveEntry zipFile = archive.CreateEntry(path, CompressionLevel.Fastest);
            using var lz4Stream = LZ4Stream.Encode(zipFile.Open(), LZ4Level.L12_MAX);
            ms.CopyTo(lz4Stream);
        }
    }
}