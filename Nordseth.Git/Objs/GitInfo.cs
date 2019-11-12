using System;
using System.Collections.Generic;
using System.Text;

namespace Nordseth.Git
{
    public class GitInfo
    {
        public string CommitId { get; set; }
        public string CommitMessage { get; set; }
        public string CommitAuthor { get; set; }
        public string CommitDate { get; set; }
        public string Branch { get; set; }
        public string CommitDescription { get; set; }
        public string OriginUrl { get; set; }

        public override string ToString()
        {
            return $@"CommitId: {CommitId}
CommitMessage:{CommitMessage}
CommitAuthor:{CommitAuthor}
CommitDate:{CommitDate}
Branch:{Branch}
CommitDescription:{CommitDescription}
OriginUrl:{OriginUrl}";
        }
    }
}
