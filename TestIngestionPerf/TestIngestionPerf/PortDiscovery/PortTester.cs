using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TestIngestionPerf
{
    public class PortTester : IPortTester
    {
        public async Task<bool> IsPortOpenAsync(string host, int port, TimeSpan connectTimeout)
        {
            using (var tcpClient = new TcpClient())
            {
                var connectTask = tcpClient.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(connectTimeout);

                var firstTask = await Task.WhenAny(connectTask, timeoutTask);
                if (firstTask == timeoutTask)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
