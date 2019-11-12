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
        [DataRow("testrepo", "1d098381751ed39164c9ad69028b756ee2bd9580")]
        [DataRow("testrepo", "611dad5ea0926ac53ed2c90b3e401366a90d00f3")]
        [DataRow("testrepo", "9675d3a09d4f5ff1b51a899f754afbbc55f3b319")]
        [DataRow("testrepo", "77a5c63d9146730d34cf812faa75a627c9446e4b")]
        // other object types
        public void Pack_ReadPackEntryHeader(string repoConfigName, string hash)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
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
        [DataRow("testrepo", "1d098381751ed39164c9ad69028b756ee2bd9580")]
        [DataRow("testrepo", "611dad5ea0926ac53ed2c90b3e401366a90d00f3")]
        [DataRow("testrepo", "9675d3a09d4f5ff1b51a899f754afbbc55f3b319")]
        [DataRow("testrepo", "77a5c63d9146730d34cf812faa75a627c9446e4b")]
        public void Pack_Read_Packed_Commits(string repoConfigName, string hash)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
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
        [DataRow("testrepo", "88b4255b27935af148b9d97f462dd71c1c0746eb")]
        [DataRow("testrepo", "36f26e55bdb06abdc413c6bb314d472835a4277e")]
        [DataRow("testrepo", "1ff0c423042b46cb1d617b81efb715defbe8054d")]
        public void Pack_Read_Packed_Blob(string repoConfigName, string hash)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
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
        [DataRow("testrepo", "1241a1c8178c18a27e190bfbee0112e2afe45bba")]
        [DataRow("testrepo", "3e938e3b41ef649c4c5c5139d2e8a8278fd29df1")]
        [DataRow("testrepo", "595cad9a557d5d699c84b02e0926f616937b9ea2")]
        [DataRow("testrepo", "82bc46048a78dffc092e90623cca894658d30c40")]
        [DataRow("testrepo", "9675d3a09d4f5ff1b51a899f754afbbc55f3b319")]
        [DataRow("testrepo", "b7812885653ce3229a6d231042be97d941239684")]
        [DataRow("testrepo", "b559165fd2b3277f8c800b4eb116ab7a5a1a3b3c")]
        [DataRow("testrepo", "ee7bf86071faa5f32372bbdc5b9d161f482bcf68")]
        [DataRow("testrepo", "fb312aac3be07cc686826bbda8cb8073f15a7f71")]
        public void Pack_Read_Pack_Entry_With_Refs(string repoConfigName, string hash)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
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
