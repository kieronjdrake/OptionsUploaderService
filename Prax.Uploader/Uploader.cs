using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;
using Newtonsoft.Json;
using NLog;
using Prax.Aspect;
using Prax.Utils;

namespace Prax.Uploader
{
    public class UploadRunner {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly List<IServerFacade> _aspectServers;
        private readonly IServerFacade _primaryAspect;
        private readonly IUploaderConfig _config;
        private readonly IMessageSink _uploadMessageSink;
        private readonly IDateTimeProvider _dtp;
        private readonly List<IInputSource> _inputSources;
        private readonly Dictionary<AspectEnvironment, IUploadQueue> _uploadQueues;

        public UploadRunner(IEnumerable<IServerFacade> aspectServers,
                            IEnumerable<IInputSource> inputSources,
                            IUploaderConfig uploaderConfig,
                            IMessageSink uploadMessageSink,
                            IDateTimeProvider dtp,
                            IUploadQueueFactory queueFactory) {
            _aspectServers = aspectServers.ToList();
            if (_aspectServers.IsEmpty())
                throw new FatalUploaderException("Please specify at least one Aspect server to connect to");
            CheckOnlyOneServerPerAspectEnvironment();
            _primaryAspect = GetPrimaryAspectServer();

            _inputSources = inputSources.ToList();
            _config = uploaderConfig;
            _uploadMessageSink = uploadMessageSink;
            _dtp = dtp;

            _uploadQueues = _aspectServers.ToDictionary(s => s.Environment, _ => queueFactory.Create());

            Logger.Log(LogLevel.Info,
                       $"Created input sources: {string.Join(", ", _inputSources.Select(i => i.SourceName))}");
        }

        public async Task UploadOptionPrices() {
            var bankHolidayLookup = new BankHolidayLookup(dt => _primaryAspect.IsBankHoliday(dt));
            
            var optionsInstruments =
                new Lazy<List<OptionInstrument>>(() => _primaryAspect.GetOptionsInstruments());
            List<OptionInstrument> GetOptionsInstruments() {
                return optionsInstruments.Value;
            }

            try {
                var (inputNames, allPricesToUpload) =
                    await GetAllOptionPriceData(GetOptionsInstruments, bankHolidayLookup);

                if (allPricesToUpload.IsEmpty()) return;

                foreach (var server in _aspectServers) {
                    _uploadQueues[server.Environment].AddAction(() => {
                        var (result, timeTaken) = UploadImpl(allPricesToUpload, server);
                        PostUploadCompleteMessage(server, inputNames, result, timeTaken);
                        if (server.IsPrimary) {
                            _inputSources.ForEach(input => MoveProcessedFiles(input, result.Succeeded));
                        }
                    });
                }
            }
            catch (TaskCanceledException) {
                Logger.Log(LogLevel.Info, "Upload cancelled, please check Aspect to see how many options were uploaded");
                foreach (var queue in _uploadQueues.Values) {
                    queue.SetCompleted();
                }
                Logger.Log(LogLevel.Debug, "All upload queues are SetCompleted");
                _uploadMessageSink.Close();
                Logger.Log(LogLevel.Debug, "Upload message sink closed");
            }
        }

        private void CheckOnlyOneServerPerAspectEnvironment() {
            var envs = _aspectServers.Select(s => s.Environment).ToList();
            if (envs.Count != envs.Distinct().Length()) {
                throw new FatalUploaderException($"Duplicate Aspect environments in config: {string.Join(",", envs)}");
            }
        }

        private IServerFacade GetPrimaryAspectServer() {
            try {
                return _aspectServers.Single(s => s.IsPrimary);
            }
            catch (Exception) {
                throw new FatalUploaderException("Please mark exactly one Aspect endpoint as isPrimary=true");
            }
        }

        private void MoveProcessedFiles(IInputSource input, bool uploadSucceeded) {
            try {
                // Always move to the Error folder if there's an error, otherwise move if config flag is set
                if (!uploadSucceeded || _config.MarkProcessedOnceUploaded) {
                    input.InputDataUploaded(uploadSucceeded);
                }
            }
            catch (Exception ex) {
                Logger.Log(LogLevel.Warn, $"Data source cleanup failed for {input.SourceName}: {ex.Message}");
            }
        }

        private async Task<(string inputNames, List<OptionPriceData> data)> 
            GetAllOptionPriceData(Func<List<OptionInstrument>> getOptionInstruments,
                                  BankHolidayLookup bankHolidayLookup) {
            var allTasks = _inputSources.Select(async s => {
                var opd = await GetOptionPriceData(s, getOptionInstruments, bankHolidayLookup);
                return match(opd, 
                             Some,
                             () => {
                                 MoveProcessedFiles(s, uploadSucceeded: false);
                                 return None;
                             });
            });
            var res = await Task.WhenAll(allTasks);
            var hasData = res.Somes().Where(d => d.data.Any()).ToList();
            var names = hasData.Select(x => x.name);
            var data = hasData.Select(x => x.data);
            return (string.Join(",", names), data.SelectMany(xs => xs).ToList());
        }

        // Returns Some if the input source read succeeds (even if the input is empty), and None if there was an error
        private async Task<Option<(string name, List<OptionPriceData> data)>> 
            GetOptionPriceData(IInputSource input,
                               Func<List<OptionInstrument>> getOptionInstruments,
                               BankHolidayLookup bankHolidayLookup) {
            try {
                Logger.Log(LogLevel.Debug, $"Checking for input data from {input.SourceName}");

                var (inputDescription, inputData) = await input.GetInputData();

                var optionData = InputMapper.Map(inputData, _config.DefaultPricingGroup, getOptionInstruments,
                                                 new TradeDateMapper(_config.TradeDateMapping, _dtp, bankHolidayLookup));

                var toUpload = optionData.Where(
                    od => UploadUtils.ArePriceDatesValidForUpload(
                              _dtp.Today, od.ExpirationDate, od.TradeDate, od.StripDate, od.IsBalMoOrCso));

                OptionalParam(_config.OptionsToSkip).IfSome(n => toUpload = toUpload.Skip(n));
                OptionalParam(_config.MaxOptionsToUpload).IfSome(n => toUpload = toUpload.Take(n));

                var dataList = toUpload.ToList();
                if (StaleTradesShouldPreventUpload(dataList, bankHolidayLookup, inputDescription, input.SourceName)) {
                    return None;
                }
                return (input.SourceName, dataList);
            }
            catch (Exception ex) when (ex is FatalUploaderException || ex is TaskCanceledException) {
                throw; // Don't swallow these, we want the calling code to deal with them
            }
            catch (Exception ex) {
                Logger.Log(LogLevel.Error, ex, $"Exception when reading input source: {ex.Message}");
                return None;
            }
        }

        private const int MaxStaleTradeDetailsToLog = 50;

        private bool StaleTradesShouldPreventUpload(List<OptionPriceData> dataList, BankHolidayLookup bankHolidayLookup,
                                                    string inputDescription, string sourceName) {
            if (_config.ForceUploadOldTradeDates) return false;

            var optionsWithStaleTradeDate =
                dataList.Where(p => !p.IsTradeDateMoreRecentThanPreviousWorkingDay(_dtp.Today, bankHolidayLookup)).ToList();
            if (!optionsWithStaleTradeDate.Any()) return false;

            Logger.Error(
                "{ntrades} stale trade dates found for {sourceName} in {inputDescription} and forceUploadOldTradeDates not set, upload aborted. Details: [ {priceJson} ]",
                optionsWithStaleTradeDate.Count, sourceName, inputDescription,
                string.Join(" , ",
                            optionsWithStaleTradeDate.Take(MaxStaleTradeDetailsToLog).Select(JsonConvert.SerializeObject)));
            return true;
        }

        private (UploadResult result, TimeSpan timeTaken) 
            UploadImpl(List<OptionPriceData> dataList, IServerFacade aspectServer) {
            
            Logger.Info($"{dataList.Count} option prices to upload to {aspectServer.Environment} Aspect");
            var (uploaded, timeTaken) = Timed.RunFunction(
                () => aspectServer.UploadOptionPrices(dataList,
                                                      _config.UploadChunkSize,
                                                      OptionalParam(_config.WaitIntervalBetweenBatchesMs),
                                                      _config.DryRun));
            Logger.Info("Uploaded option prices to {env} in {timeTaken}: {s} successes, {i} ignored and {f} failures", 
                        aspectServer.Environment, timeTaken, uploaded.Successes, uploaded.Ignored, uploaded.Failures);

            return (uploaded, timeTaken);
        }

        private void PostUploadCompleteMessage(IServerFacade s, string inputNames, UploadResult result, TimeSpan timeTaken) {
            var bld = new StringBuilder();
            bld.AppendLine($"Upload of {inputNames} option settlement prices to {s.Environment} completed at {_dtp.Now}");
            var (successes, ignored, failures) = result;
            bld.AppendLine($"{successes} successes, {ignored} ignored, {failures} failures");
            bld.AppendLine($"Time taken: {timeTaken}");
            _uploadMessageSink.SendMessage(bld.ToString(), s.IsPrimary);
        }

        private static Option<int> OptionalParam(int? p) {
            return p.HasValue && p.Value > 0 ? Some(p) : None;
        }
    }
}
