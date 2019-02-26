using System.Collections.Generic;
using System.Linq;
using Prax.Aspect;
using LanguageExt;

namespace Prax.Uploader {
    public class InstrumentLookup {
        private readonly HashMap<string, OptionInstrument> _eoMap;
        private readonly HashMap<string, OptionInstrument> _etoMap;
        private readonly HashMap<string, OptionInstrument> _aoMap;

        public InstrumentLookup(IReadOnlyCollection<OptionInstrument> optionInstruments) {
            var eoInstruments = optionInstruments.Where(i => i.InstrumentType == OptionInstrumentType.EO);
            var etoInstruments = optionInstruments.Where(i => i.InstrumentType == OptionInstrumentType.ETO);
            var aoInstruments = optionInstruments.Where(i => i.InstrumentType == OptionInstrumentType.AO);
            _eoMap = DistinctCodesMap(eoInstruments);
            _etoMap = DistinctCodesMap(etoInstruments);
            _aoMap = DistinctCodesMap(aoInstruments);
        }

        public Option<OptionInstrument> Lookup(string code) {
            // EO -> ETO -> AO lookup. This logic is taken from the option uploader workbench, which in
            // turn takes it from the OptionCity Option Upoloader.
            return _eoMap.Find(code) | _etoMap.Find(code) | _aoMap.Find(code);
        }

        private static HashMap<string, OptionInstrument> DistinctCodesMap(IEnumerable<OptionInstrument> xs) {
            return new HashMap<string, OptionInstrument>(xs.Distinct(new CompareCode()).Select(i => (i.Code, i)));
        }

        private class CompareCode : IEqualityComparer<OptionInstrument> {
            public bool Equals(OptionInstrument x, OptionInstrument y) {
                return x?.Code == y?.Code;
            }

            public int GetHashCode(OptionInstrument obj) {
                return obj.Code.GetHashCode();
            }
        }
    }
}