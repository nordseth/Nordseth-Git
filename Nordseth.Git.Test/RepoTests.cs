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
        [DataRow("refs/tags/v1.7.1")]
        [DataRow("refs/remotes/origin/maint/v1.9")]
        public void Repo_Enumerate_PackedRefs(string expectedRef)
        {
            var repo = new Repo(TestHelper.RepoPath);
            var refs = repo.EnumeratePackedRefs().ToList();

            Console.WriteLine($"found {refs.Count} packed refs");

            var foundRef = refs.FirstOrDefault(r => r.Item1 == expectedRef);
            Assert.IsNotNull(foundRef);
            Console.WriteLine($"{foundRef.name} = {foundRef.hash}");
            Assert.AreEqual(expectedRef, foundRef.name);
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
        [DataRow("refs/tags/v1.7.1")]
        [DataRow("refs/tags/v1.9.1")]
        public void Repo_Get_Ref(string refName)
        {
            var repo = new Repo(TestHelper.RepoPath);
            var hash = repo.FindRef(refName);

            Console.WriteLine($"{refName} = {hash}");
            Assert.IsNotNull(hash);
        }

        [TestMethod]
        [DataRow("338e6fb681369ff0537719095e22ce9dc602dbf0")]
        [DataRow("58d9363f02f1fa39e46d49b604f27008e75b72f2")]
        public void Repo_Read_Commit(string hash)
        {
            var repo = new Repo(TestHelper.RepoPath);
            var commit = repo.GetCommit(hash);

            Assert.IsNotNull(commit);
            Console.WriteLine(commit);
        }

        [TestMethod]
        [DataRow("009b917af7ee2700faf624dc339c2e34d41e754e")]
        [DataRow("3a10be144189e635044782d76888e40d1d862afa")]
        [DataRow("ca761c2a1767ebea1640c3004a402b097431bfee")]
        [DataRow("ea4539f35d42ffe0ece5d5d18fa3cc4108fdb775")]
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
