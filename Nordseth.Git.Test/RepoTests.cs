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
        public void Repo_Open()
        {
            var repo = new Repo(TestHelper.RepoPath);
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
        public void Repo_Read_Config()
        {
            var repo = new Repo(TestHelper.RepoPath);
            var config = repo.LoadConfig();

            Console.WriteLine(config);
        }

        [TestMethod]
        public void Repo_Enumerate_Refs()
        {
            var repo = new Repo(TestHelper.RepoPath);
            var refs = repo.EnumerateRefs().ToList();

            foreach (var r in refs)
            {
                Console.WriteLine($"{r.Item1} = {r.Item2}");
            }
        }

        [TestMethod]
        public void Repo_Get_Head()
        {
            var repo = new Repo(TestHelper.RepoPath);
            var (refName, hash) = repo.GetHead();
            Assert.IsNotNull(hash);

            Console.WriteLine($"{refName} = {hash}");
        }

        [TestMethod]
        [DataRow("refs/heads/main")]
        [DataRow("refs/remotes/origin/main")]
        [DataRow("refs/tags/v0.11.0")]
        [DataRow("refs/tags/v0.16.0")]
        public void Repo_Get_Ref(string refName)
        {
            var repo = new Repo(TestHelper.RepoPath);
            var hash = repo.FindRef(refName);

            Console.WriteLine($"{refName} = {hash}");
            Assert.IsNotNull(hash);
        }

        [TestMethod]
        [DataRow("69438c4b92b41d8971afe7cde933add34d148d4a")]
        [DataRow("e4b897ff5d1ca5177ec5053c08c717df332320b1")]
        public void Repo_Read_Commit(string hash)
        {
            var repo = new Repo(TestHelper.RepoPath);
            var commit = repo.GetCommit(hash);

            Assert.IsNotNull(commit);
            Console.WriteLine(commit);
        }

        [TestMethod]
        [DataRow("ce33b743f107cd59efec00bcabd094e01ec9826d")]
        [DataRow("f1fffd4099c89ef8bee315430afbd6c5ba3ed42a")]
        [DataRow("3a10be144189e635044782d76888e40d1d862afa")]
        public void Repo_Read_Tree(string hash)
        {
            var repo = new Repo(TestHelper.RepoPath);
            var tree = repo.GetTree(hash);

            Assert.IsNotNull(tree);
            Assert.IsTrue(tree.Any());
            foreach (var i in tree)
            {
                Console.WriteLine(i);
            }
        }

        [TestMethod]
        [DataRow(5)]
        public void Repo_Read_Commit_Log(int number)
        {
            var repo = new Repo(TestHelper.RepoPath);
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
