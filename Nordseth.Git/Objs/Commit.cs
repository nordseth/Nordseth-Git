using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nordseth.Git
{
    public class Commit
    {
        public string Id { get; set; }
        public string Tree { get; set; }
        public IEnumerable<string> Parents { get; set; }
        public Signature Author { get; set; }
        public Signature Committer { get; set; }
        public string Message { get; set; }
        public string MessageShort { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (Id != null)
            {
                builder.AppendLine($"commit {Id}");
            }

            if (Tree != null)
            {
                builder.AppendLine($"tree {Tree}");
            }

            if (Parents != null)
            {
                foreach (var parent in Parents)
                {
                    builder.AppendLine($"parent {parent}");
                }
            }

            if (Author != null)
            {
                builder.AppendLine($"author {Author}");
            }

            if (Committer != null)
            {
                builder.AppendLine($"committer {Committer}");
            }

            if (Message != null)
            {
                builder.AppendLine();
                builder.AppendLine(Message);
            }

            return builder.ToString();
        }
    }

    public class Signature
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTimeOffset When { get; set; }

        public override string ToString()
        {
            return $"{Name} <{Email}> {When:u}";
        }
    }
}