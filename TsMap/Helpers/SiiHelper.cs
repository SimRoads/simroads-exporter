using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsMap.Helpers
{
    public class SiiHelper
    {
        private const string CharNotToTrim = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";

        public static string Trim(string src)
        {
            var startTrimIndex = 0;
            var endTrimIndex = src.Length;
            for (var i = 0; i < src.Length; i++)
            {
                if (!CharNotToTrim.Contains(src[i].ToString()))
                {
                    startTrimIndex = i + 1;
                }
                else break;
            }

            for (var i = src.Length - 1; i >= 0; i--)
            {
                if (!CharNotToTrim.Contains(src[i].ToString()))
                {
                    endTrimIndex = i;
                }
                else break;
            }

            if (startTrimIndex == src.Length || startTrimIndex >= endTrimIndex) return "";
            return src.Substring(startTrimIndex, endTrimIndex - startTrimIndex);
        }

        public static (bool Valid, string Key, string Value) ParseLine(string line)
        {
            line = Trim(line);
            if (!line.Contains(":") || line.StartsWith("#") || line.StartsWith("//")) return (false, line, line);
            var key = Trim(line.Split(':')[0]);
            var val = line.Split(':')[1];
            if (val.Contains("//"))
            {
                var commentIndex = val.LastIndexOf("//", StringComparison.OrdinalIgnoreCase);
                val = val.Substring(0, commentIndex);
            }

            val = Trim(val);
            return (true, key, val);
        }
    }
}
