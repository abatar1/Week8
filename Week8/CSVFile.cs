using System.Collections.Generic;
using System.IO;

namespace Week8
{
    public class CSVFile
    {
        public IEnumerable<string[]> Content { get; }
        public string[] Header { get; }
        public int ParamsCount { get { return Header.Length; } }

        public CSVFile(IEnumerable<string[]> c, string[] h)
        {
            Content = c;
            Header = h;
        }

        public static CSVFile RawParseFile(StreamReader stream)
        {
            var header = stream.ReadLine()
                .Replace("\"", string.Empty)
                .Split(',');

            var content = new List<string[]>();
            while (true)
            {
                var str = stream.ReadLine();
                if (str == null)
                    break;

                content.Add(str.Split(','));
            }
            return new CSVFile(content, header);
        }
    }
}
