using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using TsMap.Exporter.Mvt;

namespace TsMap.Exporter.Data
{
    public class TranslationExporter : BaseExporter
    {
        public MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
        public List<string> SelectedKeys = new();

        public TranslationExporter(TsMapper mapper) : base(mapper)
        {
        }

        public override void Export(ZipArchive archive)
        {
            var zipArchiveEntry = archive.CreateEntry(Path.Join("translations", "keys"), CompressionLevel.Fastest);
            using (var stream = zipArchiveEntry.Open())
            {
                stream.Write(MessagePackSerializer.Serialize(SelectedKeys, Options));
            }

            foreach (var locale in mapper.Localization.GetLocales())
            {
                zipArchiveEntry = archive.CreateEntry(Path.Join("translations", locale), CompressionLevel.Fastest);
                var values = SelectedKeys.Select(key => mapper.Localization.GetLocaleValue(key, locale)).ToList();
                using (var stream = zipArchiveEntry.Open())
                {
                    stream.Write(MessagePackSerializer.Serialize(values, Options));
                }
            }

        }

    }

}
