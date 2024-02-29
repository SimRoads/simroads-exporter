using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace TsMap.Exporter.Data
{
    public class TranslationExporter(TsMapper mapper) : MsgPackExporter(mapper)
    {
        public readonly HashSet<string> SelectedKeys = ["lang_name"];

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
        }
    }
}