using System.Collections.Generic;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Prax.Utils
{
    public static class DictionaryExtensions {
        public static Option<TValue> Find<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key) {
            return d.TryGetValue(key, out var val) ? Some(val) : None;
        }
    }
}
