using System;
using NLog;

namespace Prax.Utils {
    public static class MessageSinkFactory {
        public static IMessageSink CreateMessageSink(IRabbitMqMessageSinkConfig rabbitConfig) {
            return CreateImpl(() => new RabbitMqMessageSink(rabbitConfig), "RabbitMQ");
        }

        public static IMessageSink CreateMessageSink(IEmailMessageSinkConfig emailConfig) {
            return CreateImpl(() => new EmailMessageSink(emailConfig), "Email");
        }

        private static IMessageSink CreateImpl(Func<IMessageSink> create, string description) {
            try {
                return create();
            }
            catch (Exception ex) {
                Logger.Warn(ex, $"Error creating {description} message sink: {ex.Message}. Using NullMessageSink.");
                return new NullMessageSink();
            }
        }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    }
}
