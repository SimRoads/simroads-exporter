using MessagePack;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace TsMap.Exporter.Data
{
    public class TranslationExporter : BaseExporter
    {
        public MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
        public List<string> SelectedKeys = new();

        public TranslationExporter(TsMapper mapper) : base(mapper)
        {
        }

        public object ExportTranslations(string locale)
        {
            return SelectedKeys.ToDictionary(x => x, x => Mapper.Localization.GetLocaleValue(x, locale));
        }

        public object ExportLocales()
        {
            return Mapper.Localization.GetLocales();
        }

    }

}
