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
        [DataRow("testrepo", "1241a1c8178c18a27e190bfbee0112e2afe45bba")]
        [DataRow("testrepo", "3e938e3b41ef649c4c5c5139d2e8a8278fd29df1")]
        [DataRow("testrepo", "595cad9a557d5d699c84b02e0926f616937b9ea2")]
        [DataRow("testrepo", "82bc46048a78dffc092e90623cca894658d30c40")]
        [DataRow("testrepo", "9675d3a09d4f5ff1b51a899f754afbbc55f3b319")]
        [DataRow("testrepo", "b7812885653ce3229a6d231042be97d941239684")]
        [DataRow("testrepo", "b559165fd2b3277f8c800b4eb116ab7a5a1a3b3c")]
        [DataRow("testrepo", "ee7bf86071faa5f32372bbdc5b9d161f482bcf68")]
        [DataRow("testrepo", "fb312aac3be07cc686826bbda8cb8073f15a7f71")]
        public void Delta_Read_Object(string repoConfigName, string hash)
        {
            var repo = new Repo(TestHelper.Config[repoConfigName]);
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
