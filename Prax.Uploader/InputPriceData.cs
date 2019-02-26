using System;
using LanguageExt;

namespace Prax.Uploader
{
    public class InputPriceData {
        public string InstrumentName { get; }
        public string ContractType { get; }
        public Option<string> PricingGroup { get; }
        public DateTime TradeDate { get; }
        public DateTime StripDate { get; }
        public Option<DateTime> ExpirationDate { get; }
        public decimal SettlementPrice { get; }
        public decimal StrikePrice { get; }
        public bool IsBalMoOrCso { get; }

        public InputPriceData(string instrumentName, string contractType, Option<string> pricingGroup,
                              DateTime tradeDate, DateTime stripDate, Option<DateTime> expirationDate,
                              decimal settlementPrice, decimal strikePrice, bool isBalMoOrCso) {
            InstrumentName = instrumentName;
            ContractType = contractType;
            PricingGroup = pricingGroup;
            TradeDate = tradeDate;
            StripDate = stripDate;
            ExpirationDate = expirationDate;
            SettlementPrice = settlementPrice;
            StrikePrice = strikePrice;
            IsBalMoOrCso = isBalMoOrCso;
        }
    }
}
