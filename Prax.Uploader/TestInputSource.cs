using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using NLog;

namespace Prax.Uploader {
    internal class TestInputSource : IInputSource {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public Task<(string sourceDescription, List<InputPriceData> data)> GetInputData() {
            var data = new List<InputPriceData> {
                new InputPriceData("BRN", "C", None,
                                   DateTime.Today, new DateTime(2019, 12, 01), new DateTime(2019, 11, 25),
                                   123.3m, 64m, false),
                new InputPriceData("BRN", "C", None,
                                   DateTime.Today, new DateTime(2019, 12, 01), new DateTime(2019, 11, 25),
                                   124.4m, 66m, false),

                // Third price is invalid as expired, use to test error handling
                //new InputPriceData("BRN", "C", None,
                //                   DateTime.Today, new DateTime(2018, 04, 01), new DateTime(2018, 04, 01),
                //                   125.5m, 66.5m, false),

                // Fourth price is stale (old tradeDate)
                //new InputPriceData("BRN", "C", None, new DateTime(2018, 5, 10),
                //                   new DateTime(2018, 07, 01), new DateTime(2018, 06, 25), 126.6m, 67m, false)
            };

            return Task.FromResult(("test data", data));
        }

        public void InputDataUploaded(bool successFlag) {
            Logger.Debug("TestInputSource.InputDataUploaded called with successFlag={flag}", successFlag);
        }

        public string SourceName => "TestInputSource";
    }
}