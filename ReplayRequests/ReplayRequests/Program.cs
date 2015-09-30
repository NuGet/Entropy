using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReplayRequests
{
    class Program
    {
        private static string connectionString;
        private static QueueClient client;
        private static StreamWriter requestsFile;
        static void Main(string[] args)
        {
            using (requestsFile = new StreamWriter("Rquests.txt", true))
            {
                connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
                client = QueueClient.CreateFromConnectionString(connectionString, CloudConfigurationManager.GetSetting("QueuePath"));
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                var task = ContinouslyReadFromQueue(tokenSource);
                task.Wait();
            }
        }

        private static async Task ContinouslyReadFromQueue(CancellationTokenSource cancellationSource)
        {
            //Read from the queue continously
            while (!cancellationSource.IsCancellationRequested)
            {
                await ReceiveMessageAsync(cancellationSource);               
            }
        }

        private static async Task ReceiveMessageAsync(CancellationTokenSource cancellationSource)
        {          
            var message = await client.ReceiveAsync();

            if (message != null)
            {
                cancellationSource.Token.ThrowIfCancellationRequested();
                await ProcessMessage(message);
            }
        }

        private static async Task ProcessMessage(BrokeredMessage message)
        {
            string messageBody = message.GetBody<string>();
            //Get the just the search query string
            if (messageBody.Contains("Search: ?q="))
            {
                messageBody = messageBody.Substring(messageBody.IndexOf('=') + 1);
                var responseMessage = await SendRequestToConsolidatedSearch(messageBody);
                await requestsFile.WriteLineAsync(string.Format("{0} - {1}", messageBody, responseMessage.StatusCode));
                await requestsFile.FlushAsync();           
            }              
        }

        private static async Task<HttpResponseMessage> SendRequestToConsolidatedSearch(string queryString)
        {
            HttpClient httpClient = new HttpClient();      
            return await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, CloudConfigurationManager.GetSetting("ReplayEndpoint")+ queryString));
        }
    }
}
