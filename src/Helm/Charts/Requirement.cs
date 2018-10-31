using System;
using System.Collections.Generic;
using System.Text;

namespace Helm.Charts
{
    internal class Requirement
    {
        internal class Dependency
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public string Repository { get; set; }
            public string Condition { get; set; }
        }

        public List<Dependency> Dependencies { get; set; }
    }
}
