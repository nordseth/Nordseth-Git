using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nordseth.Git
{
    public class GitConfig
    {
        public IDictionary<KeyValuePair<string, string>, IList<(string, string)>> Sections { get; set; }

        public IEnumerable<string> this[string section, string subSection, string key]
        {
            get
            {
                if (Sections.TryGetValue(new KeyValuePair<string, string>(section,subSection), out var values))
                {
                    return values.Where(v => v.Item1.Equals(key)).Select(v => v.Item2);
                }

                return null;
            }
        }

        public override string ToString()
        {
            var writer = new StringBuilder();
            foreach (var s in Sections)
            {
                if (s.Key.Value != null)
                {
                    writer.AppendLine($"[{s.Key.Key} \"{s.Key.Value}\"]");
                }
                else
                {
                    writer.AppendLine($"[{s.Key.Key}]");
                }

                foreach (var v in s.Value)
                {
                    writer.AppendLine($"\t{v.Item1} = \"{v.Item2}\"");
                }
            }

            return writer.ToString();
        }
    }
}
