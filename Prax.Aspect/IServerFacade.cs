using System;
using System.Collections.Generic;
using System.Threading;
using LanguageExt;
using Prax.Utils;

namespace Prax.Aspect
{
    public interface IServerFacade {
        List<OptionInstrument> GetOptionsInstruments();

        UploadResult UploadOptionPrices(List<OptionPriceData> opds, int chunkSize,
                                        Option<int> waitIntervalMs, bool isDryRun);

        bool IsBankHoliday(DateTime dt);

        AspectEnvironment Environment { get; }

        bool IsPrimary { get; }
    }
}
