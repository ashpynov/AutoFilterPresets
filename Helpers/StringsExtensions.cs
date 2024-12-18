
namespace System
{
    public static class StringExtensions
    {

        public static bool IsNullOrEmpty(this string source)
        {
            return string.IsNullOrEmpty(source);
        }

        public static bool IsNullOrWhiteSpace(this string source)
        {
            return string.IsNullOrWhiteSpace(source);
        }
        public static bool IsNoCaseEqual(this string first, string second)
        {
            if (first.IsNullOrEmpty() || second.IsNullOrEmpty())
            {
                return first == second;
            }

            return string.Compare(first, second, true) == 0;
        }
    }
}