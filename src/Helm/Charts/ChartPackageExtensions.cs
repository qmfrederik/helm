using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Helm.Charts
{
    public static class ChartPackageExtensions
    {
        internal static bool TryParseDependency(this string path, string basePath, string name, out string chartName)
        {
            var reg = new Regex(basePath + name + @"charts/(?<name>.+?/)", RegexOptions.Singleline);
            chartName = null;

            var match = reg.Match(path);

            if (match.Success)
            {
                chartName = match.Groups["name"].Value;
            }

            return match.Success;
        }

        internal static string GetName(this string path, string basePath)
        {
            var reg = new Regex("^" + basePath + @"(?<name>.+?/)", RegexOptions.Singleline);

            return reg.Match(path).Groups["name"].Value;
        }

        internal static bool IsMetadata(this string path, string basePath, string chartName)
        {
            var reg = new Regex("^" + basePath + chartName + "Chart.yaml$");

            return reg.IsMatch(path);
        }

        internal static bool IsValues(this string path, string basePath, string chartName)
        {
            var reg = new Regex("^" + basePath + chartName + "values.yaml$");

            return reg.IsMatch(path);
        }

        internal static bool IsTemplate(this string path, string basePath, string chartName)
        {
            var reg = new Regex("^" + basePath + chartName + "templates/.+");

            return reg.IsMatch(path);
        }
    }
}
