using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Services.Protocols;
using Newtonsoft.Json;
using NLog;
using Prax.Utils;
using LanguageExt;
using static LanguageExt.Prelude;
using Polly;

namespace Prax.Aspect
{
    // TODO This class has a lot of business logic in it that is untestable due to the tight-coupling with Aspect
    // We should "add another layer of abstraction" and create a tighter wrapper interface around AspectWs to allow
    // mocking/faking etc.

    /// <summary>
    /// Class is a friendly wrapper around the Aspect web services in AspectWs.cs
    /// </summary>
    public class ServerFacade : IServerFacade {
        private readonly Logger _logger;
        private AspectWs _service;
        private readonly Func<AspectWs> _createAspectConnection;
        private readonly CancellationToken _ct;
        private readonly int _maxRetries;
        private readonly Option<LogLevel> _maxLogLevel;
        private readonly UploadMethod _uploadMethod;

        public ServerFacade(IAspectConfig config, CancellationToken ct) {
            if (config == null) throw new ArgumentNullException(nameof(config));
            Environment = config.AspectEnvironment;
            IsPrimary = config.IsPrimary;
            _logger = LogManager.GetLogger($"Prax.Aspect.ServerFacade.{Environment}");
            _maxLogLevel = config.MaxLogLevel.ToOption().Map(mll => LogLevel.FromOrdinal((int)mll));
            _uploadMethod = config.UploadMethod;
            _ct = ct;
            try {
                var (username, password) = config.Credentials.GetCredentials();
                _createAspectConnection = () => new AspectWs {
                    CookieContainer = new CookieContainer(),
                    Url = Environment.GetServiceUrl(),
                    Credentials = new NetworkCredential(username, password),
                    PreAuthenticate = true,
                    Timeout = config.WebServiceTimeoutMinutes * 60 * 1000
                };
                _logger.Log(GetLevel(LogLevel.Info),
                            "Connecting to {env} Aspect at {url}", Environment, Environment.GetServiceUrl());
                _service = _createAspectConnection();
                _maxRetries = config.ConnectionFailureRetryCount;
            }
            catch (Exception e) {
                _logger.Fatal(e, "Could not create Aspect Service"); // Ignore max-log-level here as we're failing fast
                throw;
            }
        }

        // We get an exception like this if the web services we are talking to differ from the service in the
        // code. This is something we need to regenerate / rebuild for so fail fast.
        private static bool IsTemplateNotFoundException(Exception ex) {
            return ex is SoapHeaderException && ex.Message.StartsWith("Template not found");
        }

        private static FatalUploaderException CreateFatalException(Exception innerException) {
            return new FatalUploaderException("Unrecoverable Aspect exception in Primary Server Facade", innerException);
        }

        public List<OptionInstrument> GetOptionsInstruments() {
            var policy = CreateAspectRetryForeverPolicy("get option instruments", 30);

            List<OptionInstrument> GetOptionInstrumentsFromAspect() {
                var response = _service.getAllOptionInstruments();
                var etos = GetItems(response.Items).Select(i => (OptionInstrumentType.ETO, i.name, i.code));
                var eos = GetItems(response.Items1).Select(i => (OptionInstrumentType.EO, i.name, i.code));
                var aos = GetItems(response.Items2).Select(i => (OptionInstrumentType.AO, i.name, i.code));
                return etos.Concat(eos).Concat(aos).Select(data => new OptionInstrument(data)).ToList();
            }

            return RunAspectFunctionWithPolicy("GetOptionsInstruments", policy, GetOptionInstrumentsFromAspect);
        }

        private T RunAspectFunctionWithPolicy<T>(string name, Policy policy, Func<T> f) {
            try {              
                var (result, elapsed) = Timed.RunFunction(() => policy.Execute(cancellationToken => f(), _ct));
                _logger.Log(GetLevel(LogLevel.Debug), "{name} successful in {elapsed}", name, elapsed);
                return result;
            }
            catch (OperationCanceledException) {
                _logger.Log(GetLevel(LogLevel.Debug), "{name} operation cancelled", name);
                throw;
            }
            catch (Exception ex) {
                _logger.Log(LogLevel.Warn, // Ignore max-log-level here as we're failing fast
                            ex, $"Exception escaped retry policy for {name}: {ex.Message}.");
                if (IsPrimary) {
                    throw CreateFatalException(ex);
                }
                throw;
            }
        }

        private static IEnumerable<T> GetItems<T>(T[] items) {
            return items ?? Enumerable.Empty<T>();
        }

        public UploadResult UploadOptionPrices(List<OptionPriceData> opds, int chunkSize,
                                               Option<int> waitIntervalMs, bool isDryRun) {
            var result = UploadResult.Empty();
            int counter = 0;
            var adjective = isDryRun ? "DRY-RUN" : "Uploading to Aspect";

            var splitForUpload = SplitUploadDataIntoChunks(opds, chunkSize);
            var chunkCount = splitForUpload.Count;
            
            foreach (var uploadChunk in splitForUpload) {
                if (_ct.IsCancellationRequested) {
                    break;
                }

                var toUpload = uploadChunk.Select(ToAspectType).ToList();

                ++counter;
                _logger.Log(GetLevel(LogLevel.Debug),
                            $"{adjective} chunk {counter} ({toUpload.Count} prices) of {chunkCount} to {Environment}");

                toUpload.ForEach(p => _logger.Trace("{env} => {json}", Environment, JsonConvert.SerializeObject(p)));

                if (!isDryRun) {
                    result += UploadToAspect(toUpload);
                    waitIntervalMs.IfSome(async ms => await Task.Delay(ms, _ct));
                } else {
                    result += UploadResult.AllSucceeded(toUpload.Count);
                }
            }

            if (_ct.IsCancellationRequested) _logger.Log(GetLevel(LogLevel.Info), "Operation cancelled");
            if (result.Failures > 0) {
                _logger.Log(GetLevel(LogLevel.Warn), "Failed to upload {failures} option price(s)", result.Failures);
            }
            return result;
        }

        private List<List<OptionPriceData>> SplitUploadDataIntoChunks(List<OptionPriceData> xs, int chunkSize) {
            switch (_uploadMethod) {
                case UploadMethod.Bulk:
                    return UploadUtils.SplitForBulkUpload(xs);
                case UploadMethod.Standard:
                    return ChunkList(xs, chunkSize).ToList();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Policy CreateAspectDownRetryForeverPolicy(string name, TimeSpan ts) {
            return Policy
                .Handle<WebException>(IsAspectDownError)
                .WaitAndRetryForever(
                    _ => ts,
                    (ex, timespan) => {
                        _logger.Log(GetLevel(LogLevel.Info),
                                    "Aspect 404 when calling {name} ({message}), retrying in {ts}",
                                    name, ex.Message, timespan);
                        _service = _createAspectConnection();
                    }
                );

        }

        private Policy CreateAspectRetryForeverPolicy(string name, int retryInterval) {
            return Policy
                .Handle<Exception>(ex => !IsTemplateNotFoundException(ex)) // Fail fast for Soap Template errors
                .WaitAndRetryForever(
                    _ => TimeSpan.FromSeconds(retryInterval),
                    (ex, timespan) =>
                    _logger.Log(GetLevel(LogLevel.Info),
                                "Unable to call {name} ({message}), retrying in {ts}", name, ex.Message, timespan));
        }

        public bool IsBankHoliday(DateTime dt) {
            var policy = CreateAspectRetryForeverPolicy("isBankHoliday", 1);
            return RunAspectFunctionWithPolicy("IsBankHoliday", policy, () => _service.isBankHoliday(dt).result);
        }

        public AspectEnvironment Environment { get; }
        public bool IsPrimary { get; }

        private static readonly Lst<string> AspectDownErrors = List(
            "aspect is currently loading and optimizing its internal structures, please wait for a moment and try again",
            "the request failed with http status 404: not found",
            "the remote name could not be resolved");

        private static bool IsAspectDownError(Exception ex) {
            return ex.Message.ToLowerInvariant().ContainsAnyOf(AspectDownErrors);
        }

        // Returns the number of successful uploads
        // Uses a strategy whereby if an upload fails then retry with chunks half the size, until a chunk of 1 fails.
        // At that point record the failure and increment the failure count. Due to the magic of recursion this should
        // upload all of the prices that can be uploaded and report the correct number of failures.
        // It _should_ handle timeouts due to excessive chunk size and also errors with individual prices.
        private UploadResult UploadToAspect(List<addOptionPriceInstrumentprice> xs) {
            if (xs.IsEmpty()) return UploadResult.Empty();

            var aspectDownPolicy = CreateAspectDownRetryForeverPolicy("UploadToAspect", TimeSpan.FromMinutes(5));
            var webExceptionPolicy = Policy.Handle<WebException>().WaitAndRetry(
                retryCount: _maxRetries,
                sleepDurationProvider: _ => TimeSpan.FromSeconds(5),
                onRetry: (ex, timeSpan, context) => {
                    _logger.Log(GetLevel(LogLevel.Debug), ex,
                                "WebException in UploadToAspect, retrying in {ts}, {i} of {max}", timeSpan, context.Count+1, _maxRetries);
                    _service = _createAspectConnection();
            });

            var uploadAction = CreateUploadAction(xs);
            var policy = Policy.Wrap(webExceptionPolicy, aspectDownPolicy);
            var policyResult = policy.ExecuteAndCapture(
                ct => {
                    var elapsed = Timed.RunAction(uploadAction);
                    _logger.Log(GetLevel(LogLevel.Debug), "Option price chunk upload successful in {elapsed}ms",
                                elapsed.TotalMilliseconds);
                    return UploadResult.AllSucceeded(xs.Count);
                }, _ct);

            // We don't use RunAspectFunctionWithPolicy here as we need special handling in the general Exception case
            switch (policyResult.FinalException) {
                case SoapHeaderException ex when IsTemplateNotFoundException(ex):
                    if (IsPrimary) {
                        throw CreateFatalException(ex);
                    }
                    throw ex;
                case OperationCanceledException ex:
                    throw ex;
                case Exception ex:
                    // terminal condition, upload of a single price failed
                    if (xs.Count == 1) {
                        var (level, message, reportError) = GetErrorLogMessage(ex.Message);
                        _logger.Log(level, $"UPLOAD FAILED for {JsonConvert.SerializeObject(xs[0])}: {message}");
                        return reportError ? UploadResult.SingleFailure() : UploadResult.SingleIgnored();
                    } else {
                        // otherwise we try again with half-sized chunks
                        var (xs1, xs2) = xs.SplitIntoTwo();
                        return UploadToAspect(xs1) + UploadToAspect(xs2);
                    }
                default:
                    System.Diagnostics.Debug.Assert(policyResult.Outcome == OutcomeType.Successful);
                    return policyResult.Result;
            }
        }

        private Action CreateUploadAction(List<addOptionPriceInstrumentprice> xs) {
            switch (_uploadMethod) {
                case UploadMethod.Bulk:
                    return act(() => {
                        var firstPrice = xs.First();
                        var toUpload = new addOptionPriceBulkInstrumentprice {
                            contract = firstPrice.contract,
                            expireday = firstPrice.expireday,
                            instrument = firstPrice.instrument,
                            optiontype = firstPrice.optiontype,
                            pricedata = UploadUtils.ToBulkPriceData(xs),
                            pricinggroup = firstPrice.pricinggroup,
                            startday = firstPrice.startday
                        };
                        _service.addOptionPriceBulk(new[] {toUpload});
                    });
                case UploadMethod.Standard:
                    return act(() => _service.addOptionPrice(xs.ToArray()));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static readonly Lst<string> ErrorsNotToWorryAbout = List(
            "Cannot upload option price after contract expiration",
            "Cannot upload prices for days after option expiration");

        private (LogLevel level, string logMessage, bool reportError) GetErrorLogMessage(Option<string> message) {
            var logMessage = message.IfNone("unkown error");
            var reportError = !logMessage.ContainsAnyOf(ErrorsNotToWorryAbout);
            var logLevel = reportError ? LogLevel.Error : LogLevel.Info;
            return (GetLevel(logLevel), logMessage, reportError);
        }

        private LogLevel GetLevel(LogLevel level) {
            // Make sure we respect the maximum log level from the config, if specified
            return _maxLogLevel.Match(
                mll => level <= mll ? level : mll,
                () => level);
        }

        private static addOptionPriceInstrumentprice ToAspectType(OptionPriceData opd) {
            var aspectType = new addOptionPriceInstrumentprice {
                contract = opd.StripDate,
                day = opd.TradeDate,
                instrument = opd.Instrument.Name,
                iscall = opd.OptionType == OptionType.Call,
                optiontype = ToOptionTypeString(opd.Instrument.InstrumentType),
                price = (double)opd.SettlementPrice,
                pricinggroup = opd.PricingGroup,
                startday = opd.StripDate,
                strikeprice = (double)opd.StrikePrice
            };
            opd.ExpirationDate.IfSome(expiry => aspectType.expireday = expiry);
            return aspectType;
        }

        // Form the Aspect docs:
        // Option type (one of "option-eto", "option-cso", "option-agcso", "option-crack", "option-inter-com", "option-ao", "option-eo", "option-aro", "option-aro-freight")
        // https://myaspect.net/servlet/help?sessionid=J9EopORpMxD&page-id=entity_Price&path=/scripting/scripting.xml
        private static string ToOptionTypeString(OptionInstrumentType t) {
            switch (t) {
                case OptionInstrumentType.EO:
                    return "option-eo";
                case OptionInstrumentType.ETO:
                    return "option-eto";
                case OptionInstrumentType.AO:
                    return "option-ao";
                default:
                    throw new ArgumentOutOfRangeException(nameof(t), t, null);
            }
        }

        private static IEnumerable<List<T>> ChunkList<T>(List<T> xs, int chunkSize) {
            for (var i = 0; i < xs.Count; i += chunkSize) {
                yield return xs.GetRange(i, Math.Min(chunkSize, xs.Count - i));
            }
        }
    }
}
