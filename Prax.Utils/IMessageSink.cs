namespace Prax.Utils
{
    public interface IMessageSink {
        void Close();
        void SendMessage(string message, bool isPrimaryAspectInstance);
    }
}