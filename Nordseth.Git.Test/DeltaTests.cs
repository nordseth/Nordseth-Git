using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nordseth.Git.Test
{
    [TestClass]
    public class DeltaTests
    {
        [TestMethod]
        [DataRow("fd8430bc864cfcd5f10e5590f8a447e01b942bfe")]
        [DataRow("784bab3ee7da6133af679cae7527c4fe4a99b949")]
        [DataRow("d9a911419a68706317e4df3d3cc403e755fcae3b")]
        public void Delta_Read_Object(string hash)
        {
            var repo = new Repo(TestHelper.RepoPath);
            var objs = new ObjectReader(repo.RepoPath);

            var (type, stream) = objs.GetObject(hash);
            Assert.IsNotNull(stream);

            using (var writer = new StreamReader(stream))
            {
                Console.Write(writer.ReadToEnd());
            }
        }
    }
}
