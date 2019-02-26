namespace Prax.Utils {
    public class NullMessageSink : IMessageSink {
        public void Close() {}

        public void SendMessage(string message, bool isPrimaryAspectInstance) {}
    }
}
