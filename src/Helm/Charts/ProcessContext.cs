using System;
using System.Collections.Generic;
using System.Text;

namespace Helm.Charts
{
    internal class ProcessContext
    {
        public string BasePath { get; set; }
        public string ChartName { get; set; }
        public Hapi.Chart.Chart Chart { get; set; }
    }
}
