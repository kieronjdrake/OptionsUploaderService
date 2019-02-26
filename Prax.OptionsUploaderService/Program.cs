using System;
using System.Linq;
using System.Threading;
using Nerdle.AutoConfig;
using NLog;
using NLog.LayoutRenderers;
using Prax.Aspect;
using Prax.Uploader;
using Prax.Utils;
using SimpleInjector;
using Topshelf;
using Topshelf.SimpleInjector;

namespace Prax.OptionsUploaderService
{
    class Program {
        private static readonly Container _container = new Container();
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();

        static void Main(string[] args) {
            SetupConfigMappings();
            SetupLogging();
            SetupDI();

            try {
                var svc = HostFactory.New(x => {
                    x.UseSimpleInjector(_container);
                    x.Service<AspectOptionUploader>(s => {
                        s.ConstructUsingSimpleInjector();
                        s.WhenStarted((uploader, hostControl) => uploader.Start(hostControl));
                        s.WhenStopped((uploader, hostControl) => uploader.Stop(hostControl));
                    });

                    var serviceConfig = AutoConfig.Map<IUploaderServiceConfig>();
                    ServiceUtils.Setup(serviceConfig, x);

                    x.EnableServiceRecovery(r => {
                        // Wait 1 minute and then try to restart the service 2 times in the event of an error
                        r.RestartService(1);
                        r.RestartService(1);
                        r.OnCrashOnly();
                    });

                    // ReSharper disable once AccessToDisposedClosure
                    x.UseNLog(LogManager.LogFactory);
                });
                var rc = svc.Run();

                var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
                GetLogger().Log(LogLevel.Info, $"Service stopped, exiting with code {exitCode}");
                Environment.ExitCode = exitCode;
            }
            catch (Exception ex) {
                var message = $"TOP LEVEL EXCEPTION HANDLER: {ex}";
                GetLogger().Fatal(ex, message);
                Console.WriteLine(message);
                Environment.ExitCode = 2;
            }
            finally {
                LogManager.Shutdown();
            }
        }

        private static void SetupLogging() {
            LayoutRenderer.Register("host-address", logEvent => NetUtils.GetIp4Address()?.ToString() ?? "<unknown>");
        }

        private static void SetupConfigMappings() {
            AutoConfig.WhenMapping<ICredentialsConfig>(m => m.Map(x => x.EncryptedCredentials).Optional());
            AutoConfig.WhenMapping<ICredentialsConfig>(m => m.Map(x => x.PlainTextCredentials).Optional());
            AutoConfig.WhenMapping<IUploaderServiceConfig>(m => m.Map(x => x.EncryptedCredentials).Optional());
            AutoConfig.WhenMapping<IAspectConfig>(m => m.Map(x => x.MaxLogLevel).Optional());
        }

        private static void SetupDI() {
            var aspectEndpointConfig = AutoConfig.Map<IAspectEndpointsConfig>();
            var servers = aspectEndpointConfig.Servers.Select(c => AspectServerFactory.Create(c, _cts.Token));
            var inputConfig = AutoConfig.Map<IInputSourceConfig>();
            var inputSources = InputSourceFactory.CreateInputSources(inputConfig, _cts.Token);

            _container.Register<IDateTimeProvider, DateTimeProvider>();
            _container.Register<IUploadQueue>(() => new UploadQueue(_cts.Token));
            _container.Register<IUploadQueueFactory>(() => new UploadQueueFactory(_cts.Token));
            _container.Register<IMessageSink>(() => new CompositeMessageSink(new[] {
                MessageSinkFactory.CreateMessageSink(AutoConfig.Map<IRabbitMqMessageSinkConfig>()),
                MessageSinkFactory.CreateMessageSink(AutoConfig.Map<IEmailMessageSinkConfig>())
            }));
            _container.Register(
                () => new UploadRunner(servers,
                                       inputSources,
                                       AutoConfig.Map<IUploaderConfig>(),
                                       _container.GetInstance<IMessageSink>(),
                                       _container.GetInstance<IDateTimeProvider>(),
                                       _container.GetInstance<IUploadQueueFactory>()));
            _container.Register(() => new AspectOptionUploader(_container.GetInstance<UploadRunner>(), _cts));
        }

        private static ILogger GetLogger() {
            return LogManager.GetCurrentClassLogger();
        }
    }
}
