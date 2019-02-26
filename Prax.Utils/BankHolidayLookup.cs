using System;
using System.Collections.Concurrent;

namespace Prax.Utils
{
    /// <summary>
    /// Wraps calls to a bank holiday lookup function (e.g. via an Aspect web service) in a primitive caching layer.
    /// The cache remains valid forever, so a new instance of this class should be created to "flush" the cache
    /// </summary>
    public class BankHolidayLookup {
        private readonly Func<DateTime, bool> _bankHolidayLookupFn;
        private readonly ConcurrentDictionary<DateTime, bool> _cache = new ConcurrentDictionary<DateTime, bool>();

        public BankHolidayLookup(Func<DateTime, bool> bankHolidayLookupFn) {
            _bankHolidayLookupFn = bankHolidayLookupFn;
        }

        public bool IsBankHoliday(DateTime dt) {
            return _cache.GetOrAdd(dt, _bankHolidayLookupFn);
        }
    }
}
