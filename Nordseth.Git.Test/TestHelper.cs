using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nordseth.Git.Test
{
    public static class TestHelper
    {
        // git clone https://github.com/libgit2/libgit2.git --no-checkout
        public static string RepoPath { get; }
            = "../../../../testdata/libgit2";
    }
}
