using System;
using System.Collections.Generic;
using Prax.Utils;

namespace Prax.Uploader
{
    public class TradeDateMapper {
        private readonly Func<DateTime, IEnumerable<DateTime>> _mapperFn;

        public TradeDateMapper(TradeDateMapping mapping, IDateTimeProvider dtp, BankHolidayLookup bankHolidayLookup) {
            _mapperFn = CreateMapper(mapping, dtp, bankHolidayLookup);
        }

        public IEnumerable<DateTime> MapTradeDate(DateTime dt) {
            return _mapperFn(dt);
        }

        private static Func<DateTime, IEnumerable<DateTime>> CreateMapper(TradeDateMapping mapping,
                                                                          IDateTimeProvider dtp,
                                                                          BankHolidayLookup bankHolidayLookup) {
            switch (mapping) {
                case TradeDateMapping.AsInFile:
                    return dt => new[] {dt};
                case TradeDateMapping.NextWorkingDay:
                    return dt => new[] {dt.NextWorkingDay(bankHolidayLookup)};
                case TradeDateMapping.Today:
                    return dt => new[] {dtp.Today.AdjustToWorkingDay(bankHolidayLookup)};
                case TradeDateMapping.AsInFileAndToday:
                    return dt => new[] {dt, TodayOrNextWorkingDayIfTradeDateIsToday(dt, dtp.Today, bankHolidayLookup)};
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // If we are uploading the settlement prices the day after they are published then simply use the trade date
        // and today's date. If we are uploading the prices on the day they are published (e.g. at 11pm when the ICE
        // dat file is published) then we need to return tradeDate (which is today) and then `tomorrow` so Aspect
        // can have its Today settlement price value for the next day.
        private static DateTime TodayOrNextWorkingDayIfTradeDateIsToday(DateTime tradeDate, DateTime today,
                                                                        BankHolidayLookup bankHolidayLookup) {
            return tradeDate == today
                       ? today.NextWorkingDay(bankHolidayLookup)
                       : today.AdjustToWorkingDay(bankHolidayLookup);
        }
    }
}
