using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nordseth.Git
{
    public class ObjectParser
    {
        public Tag ReadTag(string id, Stream inputStream)
        {
            var tag = new Tag { Id = id };
            var messageBuilder = new StringBuilder();
            bool onMessage = false;

            using (var reader = new StreamReader(inputStream))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    if (!onMessage)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            onMessage = true;
                        }
                        else
                        {
                            ParseLine(line, tag);
                        }
                    }
                    else
                    {
                        if (tag.MessageShort == null)
                        {
                            tag.MessageShort = line;
                            messageBuilder.Append(line);
                        }
                        else
                        {
                            messageBuilder.AppendLine();
                            messageBuilder.Append(line);
                        }
                    }
                }
            }

            tag.Message = messageBuilder.ToString();

            return tag;
        }

        public Commit ReadCommit(string id, Stream inputStream)
        {
            var commit = new Commit { Id = id, Parents = new List<string>() };
            var messageBuilder = new StringBuilder();
            bool onMessage = false;

            using (var reader = new StreamReader(inputStream))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    if (!onMessage)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            onMessage = true;
                        }
                        else
                        {
                            ParseLine(line, commit);
                        }
                    }
                    else
                    {
                        if (commit.MessageShort == null)
                        {
                            commit.MessageShort = line;
                            messageBuilder.Append(line);
                        }
                        else
                        {
                            messageBuilder.AppendLine();
                            messageBuilder.Append(line);
                        }
                    }
                }
            }

            commit.Message = messageBuilder.ToString();

            return commit;
        }

        // Andreas Nordseth <anordseth@gmail.com> 1572909779 +0100
        public Signature ParseSignature(string line)
        {
            int emailStartSep = line.IndexOf('<');
            int emailEndSep = line.IndexOf('>');

            if (emailStartSep < 0 || emailEndSep < 0 || emailEndSep < emailStartSep)
            {
                return new Signature { Name = line };
            }

            var signature = new Signature
            {
                Name = line.Substring(0, emailStartSep),
                Email = line.Substring(emailStartSep + 1, emailEndSep - emailStartSep - 1),
            };

            int timeSep = line.IndexOf(' ', emailEndSep + 2);

            if (timeSep > 0
                && long.TryParse(line.Substring(emailEndSep + 2, timeSep - emailEndSep - 2), out long ts)
                && int.TryParse(line.Substring(timeSep, 3), out int tzH)
                && int.TryParse(line.Substring(timeSep + 3, 2), out int tzM))
            {
                var utcTime = DateTimeOffset.FromUnixTimeSeconds(ts);
                var time = new DateTimeOffset(utcTime.DateTime, new TimeSpan(tzH, (tzH >= 0 ? 1 : -1) * tzM, 0));
                signature.When = time;
            }

            return signature;
        }

        public IEnumerable<Tree> ReadTree(Stream inputStream)
        {
            var memoryStream = new MemoryStream();
            inputStream.CopyTo(memoryStream);

            var buffer = memoryStream.ToArray();

            int i = 0;
            while (true)
            {
                int stringTerm = Array.IndexOf<byte>(buffer, 0, i);
                if (stringTerm < 0)
                {
                    yield break;
                }

                // todo split
                string tmp = Encoding.UTF8.GetString(buffer, i, stringTerm - i);
                var split = tmp.Split(new[] { ' ' }, 2);

                yield return new Tree
                {
                    Mode = split.FirstOrDefault(),
                    Name = split.Skip(1).FirstOrDefault(),
                    Ref = BitConverter.ToString(buffer, stringTerm + 1, 20).Replace("-", string.Empty).ToLowerInvariant()
                };

                i = stringTerm + 21;
            }
        }

        // tree d8329fc1cc938780ffdd9f94e0d364e0ea74f579
        // parent fa386bf93034ed4ce100ba5df6f41685f81b5659
        // author Andreas Nordseth <anordseth@gmail.com> 1572909779 +0100
        // committer Andreas Nordseth <anordseth@gmail.com> 1572909779 +0100
        private void ParseLine(string line, Commit commit)
        {
            int seperator = line.IndexOf(' ');
            if (seperator < 0)
            {
                // throw invalid?
                return;
            }

            string type = line.Substring(0, seperator).ToLowerInvariant();
            string content = line.Substring(seperator + 1);
            switch (type)
            {
                case "tree":
                    commit.Tree = content;
                    break;
                case "parent":
                    ((List<string>)commit.Parents).Add(content);
                    break;
                case "author":
                    commit.Author = ParseSignature(content);
                    break;
                case "committer":
                    commit.Committer = ParseSignature(content);
                    break;
                default:
                    // throw invalid?
                    break;
            }
        }

        // object 71be0e2c84f3d807ab41a409836e332d6f1800cd
        // type commit
        // tag atest-2018-03-09
        // tagger t793508 <andreas.nordseth@canaldigital.no> 1535982439 +0200
        // 
        // first atest version for R8
        private void ParseLine(string line, Tag tag)
        {
            int seperator = line.IndexOf(' ');
            if (seperator < 0)
            {
                // throw invalid?
                return;
            }

            string type = line.Substring(0, seperator).ToLowerInvariant();
            string content = line.Substring(seperator + 1);
            switch (type)
            {
                case "object":
                    tag.Commit = content;
                    break;
                case "type":
                    //tag.Type = content;
                    break;
                case "tagger":
                    tag.Tagger = ParseSignature(content);
                    break;
                case "tag":
                    tag.Name = content;
                    break;
                default:
                    // throw invalid?
                    break;
            }
        }
    }
}
