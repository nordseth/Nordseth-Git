using System;
using System.Collections.Generic;
using System.Text;

namespace Nordseth.Git
{
    public class Tag
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Commit { get; set; }
        public string Message { get; set; }
        public string MessageShort { get; set; }
        public Signature Tagger { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (Id != null)
            {
                builder.AppendLine($"id {Id}");
            }

            if (Name != null)
            {
                builder.AppendLine($"tag {Name}");
            }

            if (Commit != null)
            {
                builder.AppendLine($"object {Commit}");
                builder.AppendLine($"type commit");
            }

            if (Tagger != null)
            {
                builder.AppendLine($"tagger {Tagger}");
            }

            if (Message != null)
            {
                builder.AppendLine();
                builder.AppendLine(Message);
            }

            return builder.ToString();
        }
    }
}
