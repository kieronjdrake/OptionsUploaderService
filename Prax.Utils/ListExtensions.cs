using System;
using System.Collections.Generic;

namespace Prax.Utils
{
    public static class ListExtensions {
        public static (List<T> list1, List<T> list2) SplitIntoTwo<T>(this List<T> xs) {
            if (xs.IsNullOrEmpty()) return (new List<T>(), new List<T>());
            if (xs.Count == 1) return (xs, new List<T>());

            var lsize = xs.Count;
            var idx = (int)Math.Ceiling(lsize / 2.0);
            return (xs.GetRange(0, idx), xs.GetRange(idx, lsize - idx));
        }
    }
}
