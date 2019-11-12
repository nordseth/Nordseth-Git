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
        [DataRow("testrepo")]
        [DataRow("testrepo2")]
        [DataRow("testrepo3")]
        public void GitInfo_Head(string repoConfigName)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var info = repo.GetGitInfo();

            Console.WriteLine(info);
        }

        [TestMethod]
        [DataRow("testrepo")]
        [DataRow("testrepo2")]
        [DataRow("testrepo3")]
        public void GitInfo_Describe_Commit(string repoConfigName)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var head = repo.GetHead();
            string description = repo.DescribeCommit(head.hash);
            Console.WriteLine(description);
        }
    }
}
