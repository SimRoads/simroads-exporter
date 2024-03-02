using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace TsMap.Exporter.Data
{
    public class TranslationExporter : MsgPackExporter
    {
        public readonly HashSet<string> SelectedKeys = ["lang_name"];
        public readonly HashSet<string> MissingKeys = [];
        private readonly HashSet<string> _availableKeys;
        
        public TranslationExporter(TsMapper mapper) : base(mapper)
        {
            _availableKeys = new(mapper.Localization.GetLocaleKeys());
        }
        

        public void AddKey(string key)
        {
            if (!_availableKeys.Contains(key)) MissingKeys.Add(key);
            else SelectedKeys.Add(key);
        }

        public override void Export(ZipArchive archive)
        {
            WriteMsgPack(archive, Path.Join("json", "translations", "keys.msgpack"), new
            {
                keys = SelectedKeys,
                locales = Mapper.Localization.GetLocales().ToDictionary(loc => loc,
                    loc => Mapper.Localization.GetLocaleValue("lang_name", loc))
            });

            foreach (var locale in Mapper.Localization.GetLocales())
            {
                if (locale != "None")
                    WriteMsgPack(archive, Path.Join("json", "translations", "locales", locale + ".msgpack"),
                        SelectedKeys.Select(key => Mapper.Localization.GetLocaleValue(key, locale)).ToList());
            }

            var fileEntry = archive.CreateEntry(Path.Join("json", "translations", "missing_keys.txt"));
            using var writer = new StreamWriter(fileEntry.Open());
            foreach (var key in MissingKeys)
            {
                writer.WriteLine(key);
            }
        }
    }
}