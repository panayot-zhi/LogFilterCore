using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogFilterCore.Utility
{
    public static class Extensions
    {
        public static bool HasPlaceholder(this string s)
        {
            return Regex.IsMatch(s, "{\\d+}");
        }
    }
}
