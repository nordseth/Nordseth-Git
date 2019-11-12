using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nordseth.Git.Test
{
    public static class TestHelper
    {
        private static IDictionary<string, string> _config;

        public static string TestRepo => Config["testrepo"];
        public static string TestRepo2 => Config["testrepo2"];
        public static string TestRepo3 => Config["testrepo3"];

        public static IDictionary<string, string> Config
        {
            get
            {
                if (_config == null)
                {
                    LoadConfig();
                }

                return _config;
            }
        }

        private static void LoadConfig()
        {
            var content = File.ReadAllText("../../../../config.user");
            _config = Newtonsoft.Json.JsonConvert.DeserializeObject<IDictionary<string, string>>(content);
        }
    }
}
