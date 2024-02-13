using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TsMap.Common;
using TsMap.FileSystem;
using TsMap.Helpers;

namespace TsMap
{
    public class TsCompany
    {
        public ulong Token { get; }
        public string Name { get; }
        public string SortName { get; }

        public TsCompany(string path)
        {
            var file = UberFileSystem.Instance.GetFile(path);

            if (file == null) return;

            var fileContent = file.Entry.Read();

            var lines = Encoding.UTF8.GetString(fileContent).Split('\n');

            foreach (var line in lines)
            {
                var (validLine, key, value) = SiiHelper.ParseLine(line);
                if (!validLine) continue;

                if (key == "company_permanent")
                {
                    Token = ScsToken.StringToToken(SiiHelper.Trim(value.Split('.')[2]));
                }
                else if (key == "name")
                {
                    Name = value.Split('"')[1];
                }
                else if (key == "sort_name")
                {
                    SortName = value.Split('"')[1];
                }
            }

        }
    }
}
