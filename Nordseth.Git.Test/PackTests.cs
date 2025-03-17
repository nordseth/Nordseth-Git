using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nordseth.Git.Test
{
    [TestClass]
    public class PackTests
    {
        [TestMethod]
        [DataRow("e6325351ceee58cf56f58bdce61b38907805544f")]
        // other object types
        public void Pack_ReadPackEntryHeader(string hash)
        {
            var repo = new Repo(TestHelper.RepoPath);
            var objs = new ObjectReader(repo.RepoPath);
            var (pack, offset) = objs.FindPackObject(hash);

            var packReader = new PackReader(repo.RepoPath);
            if (pack != null)
            {
                var entry = packReader.ReadPackEntryHeader(pack, offset);
                Console.WriteLine(entry);
            }
            else
            {
                Console.WriteLine($"not found");
            }
        }

        [TestMethod]
        [DataRow("e6325351ceee58cf56f58bdce61b38907805544f")]
        [DataRow("800980cc6d1a7d3bd1b68955ca07a52c331043e8")]
        [DataRow("921e3a68e26ad23d9c5b389fdc61c9591bdc4cff")]
        [DataRow("c5b97d5ae6c19d5c5df71a34c7fbeeda2479ccbc")]
        public void Pack_Read_Packed_Commits(string hash)
        {
            var repo = new Repo(TestHelper.RepoPath);
            var objs = new ObjectReader(repo.RepoPath);
            var (pack, offset) = objs.FindPackObject(hash);

            var packReader = new PackReader(repo.RepoPath);
            Assert.IsNotNull(pack);
            var (entry, stream) = packReader.ReadPackEntry(pack, offset);
            Console.WriteLine($"{pack} ({offset}) - {entry}");
            Assert.AreEqual(PackObjectType.OBJ_COMMIT, entry.Type);
            var commit = new ObjectParser().ReadCommit(hash, stream);
            Console.WriteLine(commit);
        }

        [TestMethod]
        [DataRow("d6e9cfceb0fff09ef3923c1156421d7a7a4f93fd")]
        public void Pack_Read_Packed_Blob(string hash)
        {
            var repo = new Repo(TestHelper.RepoPath);
            var objs = new ObjectReader(repo.RepoPath);
            var (pack, offset) = objs.FindPackObject(hash);

            var packReader = new PackReader(repo.RepoPath);
            Assert.IsNotNull(pack);
            var (entry, stream) = packReader.ReadPackEntry(pack, offset);
            Console.WriteLine($"{pack} ({offset}) - {entry}");
            Assert.AreEqual(PackObjectType.OBJ_BLOB, entry.Type);

            using (var reader = new StreamReader(stream))
            {
                Console.WriteLine(reader.ReadToEnd());
            }
        }

        [TestMethod]
        [Ignore]
        public void Pack_Read_Pack_Entry_With_Refs(string hash)
        {
            var repo = new Repo(TestHelper.RepoPath);
            var objs = new ObjectReader(repo.RepoPath);
            var packReader = new PackReader(repo.RepoPath);
            var entries = packReader.ReadPackEntryHeaderWithRefs(hash, objs.FindPackObject);
            Assert.IsNotNull(entries);

            foreach (var e in entries)
            {
                Console.WriteLine(e);

                if (e.Type == PackObjectType.OBJ_OFS_DELTA || e.Type == PackObjectType.OBJ_REF_DELTA)
                {
                    using (var fileStream = File.OpenRead(Path.Combine(repo.RepoPath, "objects/pack", $"{e.Pack}.pack")))
                    {
                        fileStream.Seek(e.ContentOffset, SeekOrigin.Begin);
                        var delta = DeltaStream.DescribeDelta(fileStream);
                        Console.WriteLine(delta);
                    }
                }
            }
        }
    }
}
