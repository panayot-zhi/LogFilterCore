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