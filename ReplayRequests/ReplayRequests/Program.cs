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
        public static void Main(string[] args)
        {    
            using (var requestsFile = File.CreateText("Rquests.txt"))
            {
                var connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
                var queueClient = QueueClient.CreateFromConnectionString(connectionString, CloudConfigurationManager.GetSetting("QueuePath"));
                var cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = cancellationTokenSource.Token;
                var task = ContinouslyReadFromQueue(cancellationToken, queueClient, requestsFile);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                cancellationTokenSource.Cancel();
                task.Wait();
            }
        }

        private static async Task ContinouslyReadFromQueue(CancellationToken cancellationToken, QueueClient queueClient, TextWriter requestsFile)
        {
            //Read from the queue continously
            while (!cancellationToken.IsCancellationRequested)
            {
                await ReceiveMessageAsync(cancellationToken,queueClient, requestsFile);               
            }
        }

        private static async Task ReceiveMessageAsync(CancellationToken cancellationToken,QueueClient queueClient, TextWriter requestsFile)
        {          
            var message = await queueClient.ReceiveAsync();

            if (message != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessMessage(cancellationToken, message,requestsFile);
            }
        }

        private static async Task ProcessMessage(CancellationToken cancellationToken, BrokeredMessage message, TextWriter requestsFile)
        {
            string messageBody = message.GetBody<string>();
            //Get the just the search query string
            if (messageBody.Contains("Search: ?q="))
            {
                messageBody = messageBody.Substring(messageBody.IndexOf('=') + 1);
                cancellationToken.ThrowIfCancellationRequested();
                var responseMessage = await SendRequestToConsolidatedSearch(cancellationToken, messageBody);
                await requestsFile.WriteLineAsync(string.Format("{0} - {1}", messageBody, responseMessage.StatusCode));
                await requestsFile.FlushAsync();           
            }              
        }

        private static async Task<HttpResponseMessage> SendRequestToConsolidatedSearch(CancellationToken cancellationToken, string queryString)
        {
            HttpClient httpClient = new HttpClient();
            cancellationToken.ThrowIfCancellationRequested();  
            return await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, CloudConfigurationManager.GetSetting("ReplayEndpoint")+ queryString));
        }
    }
}
