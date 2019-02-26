using System;
using System.Threading;
using System.Threading.Tasks;
using Nerdle.AutoConfig;
using NLog;
using Prax.Uploader;
using Topshelf;

namespace Prax.OptionsUploaderService
{
    // Console / service runner class, just polls for new inputs on a loop
    //
    // ReSharper disable once MemberCanBePrivate.Global
    public class AspectOptionUploader : ServiceControl {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly UploadRunner _runner;
        private readonly CancellationTokenSource _cts;
        private HostControl _hostControl;
        private Task _runnerTask;

        public AspectOptionUploader(UploadRunner runner, CancellationTokenSource cts) {
            _runner = runner;
            _cts = cts;
        }

        public bool Start(HostControl hostControl) {
            _hostControl = hostControl;
            Logger.Info("Starting service ...");
            var serviceConfig = AutoConfig.Map<IUploaderServiceConfig>();
            _runnerTask = Task.Run(() => RunLoop(serviceConfig.PollingIntervalSeconds));
            Logger.Debug("Runner task created");
            Logger.Info("Service started");
            return true;
        }

        public bool Stop(HostControl hostControl) {
            Logger.Info("Stopping service ...");
            _cts.Cancel();
            Logger.Debug("Cancellation token Cancel() called, waiting for runner task");
            _runnerTask.Wait(TimeSpan.FromSeconds(10));
            Logger.Debug("Service Stop() completed");
            return true;
        }

        private async void RunLoop(int pollingIntervalSeconds) {
            System.Diagnostics.Debug.Assert(_hostControl != null);
            try {
                while (!_cts.IsCancellationRequested) {
                    await _runner.UploadOptionPrices();
                    await Task.Delay(TimeSpan.FromSeconds(pollingIntervalSeconds), _cts.Token);
                }
                Logger.Info("Cancellation requested, shutting down");
            }
            catch (TaskCanceledException) {
                Logger.Info("Runner Task was cancelled, shutting down");
            }
            catch (Exception ex) {
                Logger.Fatal(ex, $"Unrecoverable Upload error: {ex.Message}");
                Logger.Info("Shutting down");
                _hostControl.Stop(TopshelfExitCode.AbnormalExit);
            }
        }
    }
}
