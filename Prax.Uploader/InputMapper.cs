using System;
using System.Collections.Generic;
using System.Linq;
using Prax.Aspect;
using static LanguageExt.Prelude;

namespace Prax.Uploader {
    public static class InputMapper {
        public static IEnumerable<OptionPriceData> Map(IEnumerable<InputPriceData> inputData,
                                                       string defaultPricingGroup,
                                                       Func<List<OptionInstrument>> getOptionInstruments,
                                                       TradeDateMapper mapper) {
            InstrumentLookup optionLookup = null;
            foreach (var inputPriceData in inputData) {
                optionLookup = optionLookup ?? new InstrumentLookup(getOptionInstruments());
                var mapped = ToOptionPriceData(inputPriceData, defaultPricingGroup, optionLookup, mapper);
                foreach (var optionPriceData in mapped) {
                    yield return optionPriceData;
                }
            }
        }

        private static IEnumerable<OptionPriceData> ToOptionPriceData(InputPriceData d,
                                                                string defaultPricingGroup,
                                                                InstrumentLookup optionLookup,
                                                                TradeDateMapper mapper) {
            var optionInstrument = optionLookup.Lookup(d.InstrumentName);
            var optionType = OptionTypeHelpers.FromContractTypeString(d.ContractType);
            return match(
                from ot in optionType
                from oi in optionInstrument
                select (oi, ot),
                Some: xs => {
                    var (oi, ot) = xs;
                    return mapper.MapTradeDate(d.TradeDate).Select(
                        dt => new OptionPriceData(
                                  instrument: oi,
                                  optionType: ot,
                                  tradeDate: dt,
                                  stripDate: d.StripDate,
                                  expirationDate: d.ExpirationDate,
                                  settlementPrice: d.SettlementPrice,
                                  strikePrice: d.StrikePrice,
                                  pricingGroup: d.PricingGroup.IfNone(defaultPricingGroup),
                                  isBalMoOrCso: d.IsBalMoOrCso));
                },
                None: Enumerable.Empty<OptionPriceData>);
        }
    }
}
