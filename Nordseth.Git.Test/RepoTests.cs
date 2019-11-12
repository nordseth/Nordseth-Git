using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nordseth.Git.Test
{
    [TestClass]
    public class RepoTests
    {
        [TestMethod]
        [DataRow("testrepo")]
        [DataRow("testrepo2")]
        public void Repo_Open(string repoConfigName)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
        }

        [TestMethod]
        [DataRow("../../../")]
        public void Repo_Open_Invalid(string path)
        {
            try
            {
                var repo = new Repo(path);
                Assert.Fail("Should throw exception");
            }
            catch (InvalidOperationException)
            {
                // ok
            }
        }

        [TestMethod]
        [DataRow("testrepo")]
        [DataRow("testrepo2")]
        public void Repo_Read_Config(string repoConfigName)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var config = repo.LoadConfig();

            Console.WriteLine(config);
        }

        [TestMethod]
        [DataRow("testrepo")]
        [DataRow("testrepo2")]
        public void Repo_Enumerate_Refs(string repoConfigName)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var refs = repo.EnumerateRefs().ToList();

            foreach (var r in refs)
            {
                Console.WriteLine($"{r.Item1} = {r.Item2}");
            }
        }

        [TestMethod]
        [DataRow("testrepo")]
        [DataRow("testrepo2")]
        [DataRow("testrepo3")]
        public void Repo_Get_Head(string repoConfigName)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var (refName, hash) = repo.GetHead();
            Assert.IsNotNull(hash);

            Console.WriteLine($"{refName} = {hash}");
        }

        [TestMethod]
        [DataRow("testrepo", "refs/heads/master")]
        [DataRow("testrepo", "refs/heads/dummy")]
        [DataRow("testrepo", "refs/remotes/origin/heads/master")]
        public void Repo_Get_Ref(string repoConfigName, string refName)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var hash = repo.FindRef(refName);

            Console.WriteLine($"{refName} = {hash}");
        }

        [TestMethod]
        [DataRow("testrepo2", "872206c2cb834fda85a9ec4f31b8d41929446a2c")]
        [DataRow("testrepo3", "61fad27e7ed90854eaab7d74ed913918003de4f7")]
        public void Repo_Read_Commit(string repoConfigName, string hash)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var commit = repo.GetCommit(hash);

            Assert.IsNotNull(commit);
            Console.WriteLine(commit);
        }

        [TestMethod]
        [DataRow("testrepo", "b559165fd2b3277f8c800b4eb116ab7a5a1a3b3c")]
        [DataRow("testrepo", "2e244af0aee9c373f98a54e6c0acdee1927ca2a7")]
        [DataRow("testrepo2", "fde49b2ece9452ff61f50b94a3b77f322bbedeac")]
        public void Repo_Read_Tree(string repoConfigName, string hash)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var tree = repo.GetTree(hash);

            Assert.IsNotNull(tree);
            Assert.IsTrue(tree.Any());
            foreach (var i in tree)
            {
                Console.WriteLine(i);
            }
        }

        [TestMethod]
        [DataRow("testrepo3", 5)]
        public void Repo_Read_Commit_Log(string repoConfigName, int number)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var head = repo.GetHead();
            Console.WriteLine($"HEAD: {head.Item1} = {head.Item2}");

            string commitId = head.Item2;
            for (int i = 0; i < number; i++)
            {
                var commit = repo.GetCommit(commitId);

                Assert.IsNotNull(commit);
                Console.WriteLine(commit);
                commitId = commit.Parents.FirstOrDefault();
            }
        }
    }
}
