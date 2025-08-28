using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAM.Utilities
{
    public static class PathHelper
    {
        public static string NormalizeForMySql(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            // Normalize slashes
            string normalized = path.Replace('/', '\\');

            // Remove trailing slash
            if (normalized.EndsWith("\\"))
                normalized = normalized.TrimEnd('\\');

            return normalized;
        }

        public static string ToMySqlLikePattern(string oldPath)
        {
            // Append \% (single backslash + % for SQL LIKE)
            return NormalizeForMySql(oldPath) + "\\%";
        }
    }


}
