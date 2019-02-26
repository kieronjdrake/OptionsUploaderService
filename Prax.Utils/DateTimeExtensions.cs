using System;

namespace Prax.Utils {
    public static class DateTimeExtensions {
        public static DateTime ToStartOfMonth(this DateTime dt) {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        public static DateTime PreviousWorkingDay(this DateTime dt, BankHolidayLookup bankHolidayLookup) {
            return MoveToWorkingDay(dt, bankHolidayLookup, -1);
        }

        public static DateTime NextWorkingDay(this DateTime dt, BankHolidayLookup bankHolidayLookup) {
            return MoveToWorkingDay(dt, bankHolidayLookup, 1);
        }

        public static DateTime AdjustToWorkingDay(this DateTime dt, BankHolidayLookup bankHolidayLookup) {
            return IsWeekendOrBankHoliday(dt, bankHolidayLookup) ? dt.NextWorkingDay(bankHolidayLookup) : dt;
        }

        private static DateTime MoveToWorkingDay(this DateTime dt, BankHolidayLookup bankHolidayLookup, int increment) {
            var wd = dt;
            do {
                wd = wd.AddDays(increment);
            } while (IsWeekendOrBankHoliday(wd, bankHolidayLookup));
            return wd;
        }

        private static bool IsWeekendOrBankHoliday(DateTime dt, BankHolidayLookup bankHolidayLookup) {
            return dt.IsWeekend() || bankHolidayLookup.IsBankHoliday(dt);
        }

        public static bool IsWeekend(this DateTime dt) {
            return dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;
        }
    }
}
