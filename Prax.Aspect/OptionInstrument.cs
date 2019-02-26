using System.Collections.Generic;

namespace Prax.Aspect
{
    // ReSharper disable InconsistentNaming
    public enum OptionInstrumentType { EO, ETO, AO }
    // ReSharper restore InconsistentNaming

    public class OptionInstrument {
        public OptionInstrumentType InstrumentType { get; }
        public string Name { get; }
        public string Code { get; }

        public OptionInstrument(OptionInstrumentType instrumentType, string name, string code) {
            InstrumentType = instrumentType;
            Name = name;
            Code = code;
        }

        public OptionInstrument((OptionInstrumentType instrumentType, string name, string code) data)
            : this(data.instrumentType, data.name, data.code) { }

        // We consider the Code to be the unique identifier for the instrument for mapping to Aspect
        public override bool Equals(object obj) {
            var instrument = obj as OptionInstrument;
            return instrument != null && Code == instrument.Code;
        }

        public override int GetHashCode() {
            var hashCode = 1714034946;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Code);
            return hashCode;
        }
    }
}
