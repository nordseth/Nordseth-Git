using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nordseth.Git.Test
{
    [TestClass]
    public class ObjectTests
    {
        [TestMethod]
        [DataRow("testrepo")]
        [DataRow("testrepo2")]
        public void Objects_Read_UnpackedObject_At_Repo_Head(string repoConfigName)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var head = repo.GetHead();

            Console.WriteLine($"HEAD: {head.Item1} = {head.Item2}");
            Assert.IsNotNull(head.Item2);

            var objs = new ObjectReader(repo.RepoPath);
            var (type, stream) = objs.GetUnpackedObject(head.Item2);
            Assert.IsNotNull(stream);
            Console.WriteLine($"type: {type}");
            using (var reader = new StreamReader(stream))
            {
                Console.WriteLine(reader.ReadToEnd());
            }
        }

        [TestMethod]
        [DataRow("testrepo")]
        [DataRow("testrepo2")]
        public void Objects_Read_Object_At_Repo_Head(string repoConfigName)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var head = repo.GetHead();

            Console.WriteLine($"HEAD: {head.Item1} = {head.Item2}");
            Assert.IsNotNull(head.Item2);

            var objs = new ObjectReader(repo.RepoPath);
            var (type, stream) = objs.GetObject(head.Item2);
            Assert.IsNotNull(stream);
            Console.WriteLine($"type: {type}");
            using (var reader = new StreamReader(stream))
            {
                Console.WriteLine(reader.ReadToEnd());
            }
        }

        [TestMethod]
        [DataRow("testrepo", "04796212bc3a04f6d20baf24a4a71166959bedf1")]
        [DataRow("testrepo2", "fde49b2ece9452ff61f50b94a3b77f322bbedeac")]
        public void Objects_Read_UnpackedObject(string repoConfigName, string objectId)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var objs = new ObjectReader(repo.RepoPath);
            var (type, stream) = objs.GetUnpackedObject(objectId);
            Assert.IsNotNull(stream);
            Console.WriteLine($"type: {type}");
            using (var reader = new StreamReader(stream))
            {
                Console.WriteLine(reader.ReadToEnd());
            }
        }

        [TestMethod]
        [DataRow("testrepo")]
        [DataRow("testrepo2")]
        [DataRow("testrepo3")]
        public void PackIndex_Read_Indices(string repoConfigName)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var objs = new ObjectReader(repo.RepoPath);
            objs.LoadIndex();

            Console.WriteLine($"indices: {objs.PackIndex.Count()}");
            foreach (var i in objs.PackIndex)
            {
                Console.WriteLine($"{i.Name} - v{i.Version} - {i.Objects} objects");
            }
        }

        [TestMethod]
        [DataRow("testrepo", "1d098381751ed39164c9ad69028b756ee2bd9580")]
        [DataRow("testrepo", "611dad5ea0926ac53ed2c90b3e401366a90d00f3")]
        [DataRow("testrepo", "9675d3a09d4f5ff1b51a899f754afbbc55f3b319")]
        [DataRow("testrepo", "77a5c63d9146730d34cf812faa75a627c9446e4b")]
        public void PackIndex_Find_ObjectId(string repoConfigName, string hash)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var objs = new ObjectReader(repo.RepoPath);
            var (pack, offset) = objs.FindPackObject(hash);
            Console.Write($"Search for {hash} - ");
            if (pack != null)
            {
                Console.WriteLine($"found in {pack}, offset {offset}");
            }
            else
            {
                Console.WriteLine($"not found");
            }
        }
    }
}
