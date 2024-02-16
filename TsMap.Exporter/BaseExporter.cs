using MessagePack;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;

namespace TsMap.Exporter
{
    public abstract class BaseExporter
    {
        public readonly TsMapper Mapper;


        protected BaseExporter(TsMapper mapper)
        {
            Mapper = mapper;
        }

        public virtual void Prepare() { }


    }

    public static partial class JSBaseExporter
    {
        private static MessagePackSerializerOptions _options = 
            MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
        private static TsMapper _mapperInstance;
        public static TsMapper MapperInstance
        {
            get
            {
                if (_mapperInstance == null)
                    throw new Exception("Map isn't loaded. Call LoadMap() first.");
                return _mapperInstance;
            }
        }

        [JSExport]
        public static void LoadMap(string gameDir)
        {
            _mapperInstance = new TsMapper(gameDir, new());
            Console.WriteLine(gameDir);
            _mapperInstance.Parse();
            Console.WriteLine("Map loaded");
        }

        [JSExport]
        public static void LoadMap(string gameDir, string[] modsPaths)
        {
            var mods = modsPaths.Select(x => new Mod(x)).ToList();
            _mapperInstance = new TsMapper(gameDir, mods);
            _mapperInstance.Parse();
        }

        public static Span<Byte> GetMsgPack(object obj)
        {
            return MessagePackSerializer.Serialize(obj, _options);
        }
    }

}
