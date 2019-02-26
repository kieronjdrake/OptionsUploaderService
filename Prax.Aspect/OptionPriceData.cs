using System;
using Prax.Utils;
using LanguageExt;

namespace Prax.Aspect
{
    public class OptionPriceData {
        public OptionInstrument Instrument { get; }
        public OptionType OptionType { get; }
        public DateTime TradeDate { get; }
        public DateTime StripDate { get; }
        public Option<DateTime> ExpirationDate { get; }
        public decimal SettlementPrice { get; }
        public decimal StrikePrice { get; }
        public string PricingGroup { get; }
        public bool IsBalMoOrCso { get; }

        public bool IsTradeDateMoreRecentThanPreviousWorkingDay(DateTime dt, BankHolidayLookup bankHolidayLookup) {
            return TradeDate.Date >= dt.Date.PreviousWorkingDay(bankHolidayLookup);
        }

        public OptionPriceData(OptionInstrument instrument, OptionType optionType,
                               DateTime tradeDate, DateTime stripDate, Option<DateTime> expirationDate,
                               decimal settlementPrice, decimal strikePrice,
                               string pricingGroup, bool isBalMoOrCso) {
            Instrument = instrument;
            OptionType = optionType;
            TradeDate = tradeDate;
            StripDate = stripDate;
            ExpirationDate = expirationDate;
            SettlementPrice = settlementPrice;
            StrikePrice = strikePrice;
            PricingGroup = pricingGroup;
            IsBalMoOrCso = isBalMoOrCso;
        }
    }
}
