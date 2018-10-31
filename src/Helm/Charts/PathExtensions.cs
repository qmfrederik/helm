using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Helm.Charts
{
    internal static class PathExtensions
    {
        internal static string NormalizePath(this string path)
        {
            return path.Replace('\\', '/');
        }

        internal static bool IsDependency(this string path, string basePath, string name, out string newBasePath)
        {
            var reg = new Regex(basePath + name + @"charts/.+?/.+$", RegexOptions.Singleline);

            if (reg.IsMatch(path))
            {
                newBasePath = basePath + name + @"charts/";
                return true;
            }
            else
            {
                newBasePath = string.Empty;
                return false;
            }
        }

        internal static string GetChartName(this string path, string basePath)
        {
            var reg = new Regex("^" + basePath + @"(?<name>.+?/)", RegexOptions.Singleline);

            return reg.Match(path).Groups["name"].Value;
        }

        internal static string GetUrl(this string path, string basePath, string name)
        {
            var reg = new Regex("^" + basePath + name + @"(?<url>.+)$", RegexOptions.Singleline);

            return reg.Match(path).Groups["url"].Value;
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

        internal static bool IsRequirement(this string path, string basePath, string chartName)
        {
            var reg = new Regex("^" + basePath + chartName + "requirements.yaml$");

            return reg.IsMatch(path);
        }
    }
}
