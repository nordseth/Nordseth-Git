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
        [Ignore]
        public void Tree_Read(string hash)
        {
            var repo = new Repo(TestHelper.RepoPath);
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
