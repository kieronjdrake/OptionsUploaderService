using System;
using System.Collections.Generic;
using System.Linq;

namespace Prax.Utils
{
    public static class EnumerableExtensions {
        /// <summary>
        /// Calculates the symmetric set difference (i.e. left only, right only, in both) between two collections
        /// </summary>
        /// <typeparam name="T">The type for both collections. Must have meaningful Equals semantics</typeparam>
        /// <param name="xs">The 'left' collection</param>
        /// <param name="other">The 'right' collection</param>
        /// <returns>The symmetric set difference</returns>
        public static (List<T> leftOnly, List<T> inBoth, List<T> rightOnly) SymmetricSetDifference<T>(
            this IEnumerable<T> xs, IEnumerable<T> other) {

            var lhs = xs.ToList();
            var rhs = other.ToList();
            return (
                lhs.Except(rhs).ToList(),
                lhs.Intersect(rhs).ToList(),
                rhs.Except(lhs).ToList()
            );
        }

        /// <summary>
        /// Splits the collection into two collections, containing the elements for which the given predicate returns
        /// true and false respectively. Note, eagerly evalutes the input sequence
        /// </summary>
        /// <returns>
        /// the elements that pred returns true for, those which pred returns false
        /// </returns>
        public static (List<T> trues, List<T> falses) Partition<T>(this IEnumerable<T> xs, Func<T, bool> pred) {
            var trues = new List<T>();
            var falses = new List<T>();
            foreach (var x in xs) {
                if (pred(x)) {
                    trues.Add(x);
                } else {
                    falses.Add(x);
                }
            }
            return (trues, falses);
        }

        public static bool IsEmpty<T>(this IEnumerable<T> xs) {
            return !xs.Any();
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> xs) {
            return xs == null || xs.IsEmpty();
        }

        public static IEnumerable<T> Cons<T>(T head, IEnumerable<T> tail) {
            yield return head;
            foreach (var x in tail) {
                yield return x;
            }
        }
    }
}
