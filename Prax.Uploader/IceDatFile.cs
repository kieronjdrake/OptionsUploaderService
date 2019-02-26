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
using static LanguageExt.Prelude;
using Prax.Utils;

// ReSharper disable InconsistentNaming

namespace Prax.Uploader
{
    public class IceDatFile : IInputSource {
        private readonly IFileInputSourceConfig _config;
        private readonly IDateTimeProvider _dtp;
        private readonly InputFileSource _inputFileSource;

        // ReSharper disable once ClassNeverInstantiated.Local
        [ExcludeFromCodeCoverage] // This is just used to rip data from the dat file and has no logic at all
        private sealed class DatFileLine {
            // The strange casing of the variable names is so the CsvReader automapping works with header preparation
            // of strip-spaces and to lower case. Combined this means that the ICE dat file header and the variables
            // match up exactly and so GetRecords<DatFileLine> works.
            public DateTime tradedate { get; set; }
            public string hub { get; set; }
            public DateTime strip { get; set; }
            public string contract { get; set; }
            public string contracttype { get; set; }
            public decimal? strike { get; set; }
            public decimal? settlementprice { get; set; }
            public DateTime expirationdate { get; set; }
        }

        public IceDatFile(IFileInputSourceConfig config, IFileSystemProxy fileSystem, IDateTimeProvider dtp, CancellationToken ct) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _dtp = dtp ?? throw new ArgumentNullException(nameof(dtp));
            _inputFileSource = new InputFileSource(fileSystem, SourceName, _config.InputDirectory,
                                                   _config.FileReadRetryAttempts, ct);
        }

        public string SourceName => "ICE Dat file";

        public async Task<(string sourceDescription, List<InputPriceData> data)> GetInputData() {

            await _inputFileSource.CheckInputDirectoryForInputFiles("*.dat", _config.InitialRetryDelayMs);
    
            IEnumerable<InputPriceData> ConvertCsvLines(IEnumerable<DatFileLine> lines) {
                return lines.Where(IsValidOptionPrice).Select(Convert);
            }

            return _inputFileSource.ReadAllFilesReadyForUpload(ReadDatFile, ConvertCsvLines);
        }

        private static IEnumerable<DatFileLine> ReadDatFile(TextReader textReader) {
            var csv = new CsvReader(textReader);
            csv.Configuration.Delimiter = "|";
            csv.Configuration.HasHeaderRecord = true;
            csv.Configuration.TrimOptions = TrimOptions.Trim;
            csv.Configuration.PrepareHeaderForMatch = h => h.Replace(" ", "").ToLowerInvariant();
            // ICE specifies dates in US format, so we change the CSV reader culture to en-US
            csv.Configuration.CultureInfo = CultureInfo.CreateSpecificCulture("en-US");

            return csv.GetRecords<DatFileLine>();
        }

        public void InputDataUploaded(bool uploadSucceeded) {
            _inputFileSource.MoveFilesToProcessedFolder(uploadSucceeded, SourceName);
        }

        private bool IsValidOptionPrice(DatFileLine l) {
            // Some of the rows seem to be for futures (eg no strike price) so filter those out
            return UploadUtils.IsPutOrCall(l.contracttype) &&
                   l.strike.HasValue && l.settlementprice.HasValue;
        }

        private static bool IsBalMoOrCso(string hub) {
            // From KD: You can tell a BalMo by the fact that the HUB (really an Instrument description, sort of)
            //          has " - End" in it while a CSO has " CSO " in it.
            var s = hub.ToUpperInvariant();
            return s.Contains("- END") || s.Contains(" CSO");
        }

        private static InputPriceData Convert(DatFileLine l) {
            return new InputPriceData(
                instrumentName: MapContractName(l.contract),
                contractType: l.contracttype,
                pricingGroup: None, 
                tradeDate: l.tradedate,
                stripDate: l.strip,
                expirationDate: l.expirationdate,
                settlementPrice: l.settlementprice ?? 0,
                strikePrice: l.strike ?? 0,
                isBalMoOrCso: IsBalMoOrCso(l.hub));
        }

        // These hardcoded contract mappings are from Kieron's uploader workbench in Aspect
        private static string MapContractName(string contractName) {
            switch (contractName) {
                case "B":
                    return "BRN"; // ICE Brent
                case "T":
                    return "WBS"; // ICE WTI
                case "HOF":
                    return "HO"; // ICE Heat
                default:
                    return contractName;
            }
        }
    }
}