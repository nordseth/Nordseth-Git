using System;
using System.Collections.Generic;
using System.Text;

namespace Nordseth.Git
{
    public class Tree
    {
        public string Mode { get; set; }
        public string Name { get; set; }
        public string Ref { get; set; }

        public override string ToString()
        {
            return $"{Mode} {Name} {Ref}";
        }
    }
}
