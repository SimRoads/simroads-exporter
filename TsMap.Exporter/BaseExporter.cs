using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
