using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using LanguageExt;
using static LanguageExt.Prelude;
using Prax.Utils;

namespace Prax.Uploader
{
    public class NymexOptionFile : IInputSource {
        private readonly IFileInputSourceConfig _config;
        private readonly IDateTimeProvider _dtp;
        private readonly InputFileSource _inputFileSource;

        private const decimal NYMEX_Option_Settled_OutOfTheMoney = 9_999_999M;
        private const decimal NYMEX_Option_Default_OoTM = 0.01M;

        [ExcludeFromCodeCoverage] // This is just used to rip data from the CSV file and has no logic at all
        // ReSharper disable once ClassNeverInstantiated.Local
        private sealed class CsvFileLine {
            public DateTime BizDt { get; set; }
            public string ID { get; set; }
            public string Sym { get; set; }
            public decimal? StrkPx { get; set; }
            public string SecTyp { get; set; }
            public int MMY { get; set; }
            public bool? PutCall { get; set; }
            public decimal? SettlePrice { get; set; }
            // TODO Is LastTrdDt the expiry date? If so we should add it and pass to UploadUtils.ArePriceDatesValidForUpload
        }

        private static DateTime GetStripDate(CsvFileLine l) {
            // mmy is either in the format 201807 or 20180701
            var s = l.MMY.ToString();
            var dts = s.Length == 6 ? s + "01" : s;
            return DateTime.ParseExact(dts, "yyyyMMdd", CultureInfo.CurrentCulture);
        }

        public NymexOptionFile(IFileInputSourceConfig config, IFileSystemProxy fileSystem, IDateTimeProvider dtp,
                               CancellationToken ct) {
            _config = config;
            _dtp = dtp;
            _inputFileSource = new InputFileSource(fileSystem, SourceName, _config.InputDirectory,
                                                   _config.FileReadRetryAttempts, ct);
        }

        public async Task<(string sourceDescription, List<InputPriceData> data)> GetInputData() {
            await _inputFileSource.CheckInputDirectoryForInputFiles("*.s.csv", _config.InitialRetryDelayMs);

            IEnumerable<InputPriceData> ConvertCsvLines(IEnumerable<CsvFileLine> lines) {
                return lines.Where(IsValidOptionPrice).Select(Convert);
            }

            return _inputFileSource.ReadAllFilesReadyForUpload(ReadCsvFile, ConvertCsvLines);
        }

        public void InputDataUploaded(bool uploadSucceeded) {
            _inputFileSource.MoveFilesToProcessedFolder(uploadSucceeded, SourceName);
        }

        public string SourceName => "Nymex Option File";

        private static IEnumerable<CsvFileLine> ReadCsvFile(TextReader textReader) {
            var csv = new CsvReader(textReader);
            csv.Configuration.Delimiter = ",";
            csv.Configuration.HasHeaderRecord = true;
            csv.Configuration.TrimOptions = TrimOptions.Trim;

            return csv.GetRecords<CsvFileLine>();
        }

        private static InputPriceData Convert(CsvFileLine l) {
            // LO -> Nymex WTI (ETO)
            // BZO -> Nymex Brent (ETO)
            //
            // The symbols in the csv file correspond directly to the ones in Aspect so we don't need any mappings here
            return new InputPriceData(
                instrumentName: l.ID,
                contractType: ToContractType(l.PutCall),
                pricingGroup: None, 
                tradeDate: l.BizDt,
                stripDate: GetStripDate(l),
                expirationDate: None,
                settlementPrice: (l.SettlePrice is decimal spv) ? (spv >= NYMEX_Option_Settled_OutOfTheMoney ? NYMEX_Option_Default_OoTM : spv) : 0M, // was: l.SettlePrice ?? 0,
                strikePrice: l.StrkPx ?? 0,
                isBalMoOrCso: IsBalMoOrCso(l.Sym));
        }

        private static bool IsValidOptionPrice(CsvFileLine l) {
            return l.PutCall.HasValue &&
                   l.SecTyp == "OOF" &&
                   l.StrkPx.HasValue && l.SettlePrice.HasValue;
        }

        private static string ToContractType(bool? putcall) {
            return match(
                putcall.ToOption(),
                Some: isputcall => (isputcall ? "C" : "P"),
                None: () => "");
        }

        private static bool IsBalMoOrCso(string symbol) {
            // currently we're not interested in BalMo or CSO options, if we are then we'll need to implement here
            return false;
        }
    }
}
