using System.IO.Compression;

namespace TsMap.Exporter
{
    public abstract class BaseExporter
    {
        protected readonly TsMapper mapper;

        public BaseExporter(TsMapper mapper)
        {
            this.mapper = mapper;
        }

        public abstract void Export(ZipArchive zipArchive);
    }
}
