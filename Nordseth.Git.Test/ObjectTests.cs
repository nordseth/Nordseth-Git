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
        [Ignore]
        public void Objects_Read_UnpackedObject_At_Repo_Head()
        {
            var repo = new Repo(TestHelper.RepoPath);
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
        public void Objects_Read_Object_At_Repo_Head()
        {
            var repo = new Repo(TestHelper.RepoPath);
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
        [Ignore]
        public void Objects_Read_UnpackedObject(string objectId)
        {
            var repo = new Repo(TestHelper.RepoPath);
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
        public void PackIndex_Read_Indices()
        {
            var repo = new Repo(TestHelper.RepoPath);
            var objs = new ObjectReader(repo.RepoPath);
            objs.LoadIndex();

            Console.WriteLine($"indices: {objs.PackIndex.Count()}");
            foreach (var i in objs.PackIndex)
            {
                Console.WriteLine($"{i.Name} - v{i.Version} - {i.Objects} objects");
            }
        }

        [TestMethod]
        [DataRow("69438c4b92b41d8971afe7cde933add34d148d4a")]
        [DataRow("2115230e8c45fd81640ea5a498ab3fc22e4e8925")]
        [DataRow("d853fb9f24e0fe63b3dce9fbc04fd9cfe17a030b")]
        [DataRow("37172582ec7ff9cb47c43c5d5b2334bf8c547569")]
        public void PackIndex_Find_ObjectId(string hash)
        {
            var repo = new Repo(TestHelper.RepoPath);
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
