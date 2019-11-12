using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nordseth.Git
{
    public class GitConfigReader
    {
        // https://git-scm.com/docs/git-config/1.8.2#_syntax
        // todo: unescape
        public IDictionary<KeyValuePair<string, string>, IList<(string, string)>> Read(Stream stream)
        {
            var data = new Dictionary<KeyValuePair<string, string>, IList<(string, string)>>(new SectionComparer());
            using (var reader = new StreamReader(stream))
            {
                var section = new KeyValuePair<string, string>(null, null);

                while (reader.Peek() != -1)
                {
                    var rawLine = reader.ReadLine();
                    // Trim comments
                    int commentSeperator = rawLine.IndexOfAny(new[] { '#', ';' });
                    if (commentSeperator >= 0)
                    {
                        rawLine = rawLine.Substring(0, commentSeperator);
                    }

                    var line = rawLine.Trim();

                    // Ignore blank lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    // [Section "subsection"]
                    if (line[0] == '[' && line[line.Length - 1] == ']')
                    {
                        // remove the brackets
                        var (s, ss) = GetSection(line.Substring(1, line.Length - 2));
                        section = new KeyValuePair<string, string>(s, ss);
                        continue;
                    }

                    if (!data.ContainsKey(section))
                    {
                        data[section] = new List<(string, string)>();
                    }

                    // key = value, key = "value" or key (= true)
                    int separator = line.IndexOf('=');
                    if (separator >= 0)
                    {
                        string key = line.Substring(0, separator).Trim();
                        string value = line.Substring(separator + 1).Trim();

                        // Remove quotes
                        if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
                        {
                            value = value.Substring(1, value.Length - 2);
                        }

                        data[section].Add((key, value));
                    }
                    else
                    {
                        data[section].Add((line, "true"));
                    }
                }
            }

            return data;
        }

        private (string section, string subSection) GetSection(string sectionString)
        {
            sectionString = sectionString.Trim();
            int separator = sectionString.IndexOf(' ');
            if (separator < 0)
            {
                return (sectionString, null);
            }

            string section = sectionString.Substring(0, separator).Trim();
            string subSection = sectionString.Substring(separator + 1).Trim();

            // Remove quotes
            if (subSection.Length > 1 && subSection[0] == '"' && subSection[subSection.Length - 1] == '"')
            {
                subSection = subSection.Substring(1, subSection.Length - 2);
            }

            return (section, subSection);
        }

        private class SectionComparer : IEqualityComparer<KeyValuePair<string, string>>
        {
            public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
            {
                return string.Equals(x.Key, y.Key, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Value, y.Value);
            }

            public int GetHashCode(KeyValuePair<string, string> obj)
            {
                return obj.Key?.ToLowerInvariant().GetHashCode() ?? 0 ^ obj.Value?.GetHashCode() ?? 0;
            }
        }
    }
}
