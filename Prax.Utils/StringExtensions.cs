using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using static LanguageExt.List;

namespace Prax.Utils {
    public static class StringExtensions {
        public static bool ContainsAnyOf(this string s, Lst<string> xs) {
            return exists(xs, s.Contains);
        }

        public static bool ContainsAnyOf(this string s, IEnumerable<string> xs) {
            return xs.Any(s.Contains);
        }
    }
}
