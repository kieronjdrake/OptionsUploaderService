using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Prax.Utils
{
    public static class NetUtils {
        public static IPAddress GetIp4Address() {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }
    }
}
