using System.Collections.Generic;
using System.Linq;

namespace Prax.Aspect {
    public static class UploadUtils {
        public static List<List<OptionPriceData>> SplitForBulkUpload(List<OptionPriceData> xs) {
            // The addOptionPriceBulk web service puts multiple prices into a single pricedata field,
            // where each price therein consists of tradeDate;price;strikePrice;isCall
            // All other fields are specified in specific XML elements, so we need to group by the
            // other fields: instrument, stripDate, pricingGroup, expirationDate
            return xs.GroupBy(x => new {x.Instrument, x.StripDate, x.PricingGroup, x.ExpirationDate})
                     .Select(grp => grp.ToList())
                     .ToList();
        }

        public static string ToBulkPriceData(IEnumerable<addOptionPriceInstrumentprice> xs) {
            var priceStrings =
                xs.Select(x => $"{x.day:yyyy-MM-dd};{x.price};{x.strikeprice};{(x.iscall ? "true" : "false")}");
            return string.Join(";", priceStrings);
        }
    }
}
 