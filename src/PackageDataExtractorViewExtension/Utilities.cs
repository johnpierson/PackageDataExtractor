using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PackageDataExtractor
{

    internal static class StringUtils
    {
        public static string SimplifyString(this string str)
        {
            var clean = Regex.Replace(str, "[^0-9a-zA-Z]+", "");

            return clean.ToLower().Replace(" ", "").Replace(".", "");
        }
    }

}
