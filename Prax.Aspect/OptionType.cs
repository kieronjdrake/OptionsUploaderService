using LanguageExt;
using static LanguageExt.Prelude;

namespace Prax.Aspect
{
    public enum OptionType {
        Put,
        Call
    }

    public static class OptionTypeHelpers {
        public static Option<OptionType> FromContractTypeString(string s) {
            if (string.IsNullOrEmpty(s)) return None;
            var c0 = s.ToUpperInvariant()[0];
            switch (c0) {
                case 'P':
                    return OptionType.Put;
                case 'C':
                    return OptionType.Call;
                default:
                    return None;
            }
        }
    }
}
