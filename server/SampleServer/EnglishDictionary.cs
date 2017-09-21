using System;
using System.Collections.Generic;
using System.IO;

namespace SampleServer
{
    public static class EnglishDictionary
    {
        public static string FilePath(string fileName)
        {
            var exePath = typeof(EnglishDictionary).Assembly.Location;
            var exeDir = Path.GetDirectoryName(exePath);
            return Path.Combine(exeDir, fileName);
        }

        public static SortedDictionary<string, List<string>> Load()
        {
            var dict = new SortedDictionary<string, List<string>>();
            var path = FilePath("ejdic.txt");
            foreach (var line in File.ReadAllLines(path))
            {
                var index = line.IndexOf('\t');
                if (index >= 0)
                {
                    var key = line.Substring(0, index);
                    var value = line.Substring(index + 1);
                    List<string> list;
                    if (!dict.TryGetValue(key, out list))
                    {
                        list = new List<string>();
                        dict.Add(key, list);
                    }
                    list.Add(value);
                }
            }
            return dict;
        }
    }
}
