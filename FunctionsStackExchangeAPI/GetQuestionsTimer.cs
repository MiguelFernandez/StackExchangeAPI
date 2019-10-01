using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.EventGrid;
using System.Runtime.CompilerServices;

namespace FunctionsStackExchangeAPI
{
    public class GetQuestionsTimer
    {

        private static MapperConfiguration mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new MappingProfile());
        });
        private static readonly IMapper _mapper = mappingConfig.CreateMapper();
        private static readonly string site = "stackoverflow";
        private const int timeSpanMinutes = 10;
        private static List<string> tags = new List<string>();
        private static  IConfigurationRoot config = new ConfigurationBuilder().AddEnvironmentVariables().Build();


        private static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip
        };
        private static HttpClient httpClient = new HttpClient(handler);
        [FunctionName("GetQuestionsTimer")]
        public static async System.Threading.Tasks.Task Run([TimerTrigger("0 */10 * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            //var config = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory).AddJsonFile("local.settings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables().Build();
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var endDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            log.LogInformation($"Current datetime: {endDate.ToString()}");
            var startDate = endDate - (timeSpanMinutes * 60);
            log.LogInformation($"Five minutes ago: {startDate.ToString()}");
            var tags = new List<string>();
            //~azure will get all questions with tags that contain the word azure
            tags.Add("~azure");
            
            var sortby = "creation";
            var order = "desc";
            //var sesStorageKey = config["SEStorageKey"];
            //var sesStorageName = config["SEStorageName"];
            //var seApiKey = config["SEApiKey"];

            var questions = await GetQuestionsAsync(config["SEApiKey"], startDate, endDate, tags, site, sortby, order, log);
            await SaveResponseAsync(questions);
            await SendToEventGridTopic(questions);
            //log.LogInformation(questions);

        }
        private static async System.Threading.Tasks.Task<StackExchangeResponse> GetQuestionsAsync(string key, long startDate, long endDate, List<string> tags, string site, string sortby, string order, ILogger log)
        {
            try
            {
                var tagsCSV = string.Join(",", tags);

                var requestUri = $"https://api.stackexchange.com/2.2/questions?fromdate={startDate}&todate={endDate}&order={order}&sort={sortby}&tagged={tagsCSV}&site={site}&key={key}";
                log.LogInformation(requestUri);
                var response = await httpClient.GetAsync(requestUri);
                //httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
                var responseContent = await response.Content.ReadAsStringAsync();
                var SEresponse = JsonConvert.DeserializeObject<StackExchangeResponse>(responseContent);
                return SEresponse;
            }
            catch (Exception ex)
            {

                throw;
            }

        }
        private static async Task SaveResponseAsync(StackExchangeResponse stackExchangeResponse)
        {
            //var config = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory).AddJsonFile("local.settings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables().Build();

            CloudStorageAccount storageAccount = new CloudStorageAccount(
            new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(
            config["SEStorageName"], config["SEStorageKey"]), true);

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable stackExchangeQuestionsTable = tableClient.GetTableReference("StackExchangeQuestions");

            foreach (var item in stackExchangeResponse.items)
            {

                var itemEntity = _mapper.Map<StackExchangeResponseItemEntity>(item);
                TableOperation insertOperation = TableOperation.InsertOrReplace(itemEntity);
                await stackExchangeQuestionsTable.ExecuteAsync(insertOperation);
            }


        }

        private static async Task SendToEventGridTopic(StackExchangeResponse stackExchangeResponse)
        {

            var topicHostName = "sequestionstopic.westus2-1.eventgrid.azure.net";
            var topicKey = "nPZy0jWBGjBBwGGtNCbmopNiIYfj/sR8awdtk2idaFs=";
            // Create service credential with the topic credential
            // class and custom topic access key
            var credentials = new TopicCredentials(topicKey);

            // Create an instance of the event grid client class
            var client = new EventGridClient(credentials);

            // Retrieve a collection of events
            var events = PackageEvents(stackExchangeResponse);
            if (events.Count > 0)
            {
                // Publish the events
                await client.PublishEventsAsync(
                    topicHostName,
                    events);
            }

            


        }

        private static List<EventGridEvent> PackageEvents(StackExchangeResponse stackExchangeResponse)
        {
            var events = new List<EventGridEvent>();
            var tagsCSV = string.Join(",", tags);
            foreach (var item in stackExchangeResponse.items)
            {
                var tempEvent = new EventGridEvent()
                {
                    Id = Guid.NewGuid().ToString(),
                    Data = item,
                    EventTime = DateTime.Now,
                    EventType = $"StackExchange.NewQuestion.{site}.{tagsCSV}",
                    Subject = "NewQuestion",
                    DataVersion = "1.0"
                };
                events.Add(tempEvent);
                
            }
            return events;
        }
    }
}
