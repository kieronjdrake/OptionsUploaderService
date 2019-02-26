using System;
using LanguageExt;
using Prax.Utils;

namespace Prax.Uploader
{
    public static class UploadUtils {
        public static bool ArePriceDatesValidForUpload(DateTime today, Option<DateTime> expiryDate, DateTime tradeDate,
                                                       DateTime stripDate, bool isBalMoOrCso) {
            // The logic here as explained by Kieron:
            //
            // if ExpiryDate <= TradeDate (or today) then skip.
            // If StripDate <= Beginning of current month then skip.
            // If StripDate == beginning of current month then (
            //      if it is a BalMo (balance of month) then don't skip
            //      else if it is a Calendar Spread Option (CSO) then don't skip
            //      else skip
            // )
            //
            // KD's uploader allows if tradeDate < expiration || expiration == null
            var validExpiry = expiryDate.Select(exp => (tradeDate <= exp) && (exp >= today)).IfNone(true);
            var startOfCurrentMonth = today.ToStartOfMonth();
            return validExpiry &&
                   (stripDate > startOfCurrentMonth || (stripDate == startOfCurrentMonth && isBalMoOrCso));
        }

        public static bool IsPutOrCall(string contractType) {
            if (string.IsNullOrWhiteSpace(contractType)) return false;
            var c0 = contractType.ToUpperInvariant()[0];
            return c0 == 'P' || c0 == 'C';
        }
    }
}
