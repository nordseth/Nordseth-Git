using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nordseth.Git.Test
{
    [TestClass]
    public class GitInfoTests
    {
        [TestMethod]
        public void GitInfo_Head()
        {
            var repo = new Repo(TestHelper.RepoPath);
            var info = repo.GetGitInfo();

            Console.WriteLine(info);
        }

        [TestMethod]
        public void GitInfo_Describe_Commit()
        {
            var repo = new Repo(TestHelper.RepoPath);
            var head = repo.GetHead();
            string description = repo.DescribeCommit(head.hash);
            Console.WriteLine(description);
        }
    }
}
