using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nordseth.Git.Test
{
    [TestClass]
    public class TreeTest
    {
        [TestMethod]
        [DataRow("testrepo", "b559165fd2b3277f8c800b4eb116ab7a5a1a3b3c")]
        [DataRow("testrepo", "2e244af0aee9c373f98a54e6c0acdee1927ca2a7")]
        public void Tree_Read(string repoConfigName, string hash)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
            var objs = new ObjectReader(repo.RepoPath);
            var (pack, offset) = objs.FindPackObject(hash);

            var packReader = new PackReader(repo.RepoPath);
            Assert.IsNotNull(pack);
            var (entry, stream) = packReader.ReadPackEntry(pack, offset);
            Console.WriteLine(entry);

            Assert.AreEqual(PackObjectType.OBJ_TREE, entry.Type);
            var tree = new ObjectParser().ReadTree(stream).ToList();
            foreach (var e in tree)
            {
                Console.WriteLine(e);
            }
        }
    }
}
