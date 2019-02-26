using System.ComponentModel;
using System.Net.Mail;
using NLog;

namespace Prax.Utils {
    public class EmailMessageSink : IMessageSink {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IEmailMessageSinkConfig _config;
        private readonly SmtpClient _smtpClient;

        public EmailMessageSink(IEmailMessageSinkConfig config) {
            _config = config;
            _smtpClient = new SmtpClient(_config.Host) {
                UseDefaultCredentials = false // Send anonymously
            };
            _smtpClient.SendCompleted += MessageSendCompleted;
        }

        public void Close() {
            _smtpClient?.Dispose();
        }

        public void SendMessage(string message, bool isPrimaryAspectInstance) {
            if (!isPrimaryAspectInstance) return; // Only send emails about the primary instance (e.g. Prod not Test)

            var fromAddress = new MailAddress(_config.From, "Aspect Option Uploader");
            var toAddress = new MailAddress(_config.To);
            var mailMessage = new MailMessage(fromAddress, toAddress) {
                Body = message,
                Subject = _config.Subject
            };
            _smtpClient.SendAsync(mailMessage, $"{message} -> {_config.To}");
        }

        private static void MessageSendCompleted(object sender, AsyncCompletedEventArgs e) {
            var details = e.UserState.ToString();
            if (e.Cancelled) {
                Logger.Warn($"Email send cancelled for message: {details}");
            }
            if (e.Error != null) {
                Logger.Error(e.Error, $"Error sending email with message `{details}`: {e.Error.Message}");
            } else {
                Logger.Info($"Email message sent successfully: {details}");
            }
        }
    }
}
