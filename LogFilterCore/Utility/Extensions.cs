using LogFilterCore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

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