using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.EventGrid;
using System.Net.Http;
using System.Collections.Generic;
using FunctionsStackExchangeAPI;
using System.Collections;
using O365Connectors;

namespace TeamsWebhookFunctions
{
    public static class NewQuestion
    {
        private static HttpClient httpClient = new HttpClient();
        [FunctionName("NewQuestionHttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var stackexchangeResponseItem = new StackExchangeResponseItem();
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation($"Received event: {req.Body}");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            EventGridSubscriber eventGridSubscriber = new EventGridSubscriber();
            EventGridEvent eventGridEvent = eventGridSubscriber.DeserializeEventGridEvents(requestBody)[0];

            var data = JsonConvert.SerializeObject(eventGridEvent.Data);
            stackexchangeResponseItem = JsonConvert.DeserializeObject<StackExchangeResponseItem>(data);



            var card = new ToTeams();
            var tags = string.Join('|', stackexchangeResponseItem.tags);
            var section1 = new Section() { activityTitle = stackexchangeResponseItem.title, activityText = stackexchangeResponseItem.owner.user_id.ToString(), activitySubtitle = "Tags used: " + tags, activityImage = "stackexchangeResponseItem.owner.profile_image" };

            var image = new Image();
            image.image = stackexchangeResponseItem.owner.profile_image;

            var potentialAction = new PotentialAction();
            potentialAction.context = "http://shema.org";
            potentialAction.type = "ViewAction";
            potentialAction.name = "Open in browser";
            potentialAction.target = new List<string>();
            potentialAction.target.Add(stackexchangeResponseItem.link);
            potentialAction.id = "SELink";


            card.content = new Content() { title = "New Question", summary = "Summary", sections = new List<Section>() };
            card.content.sections.Add(section1);
            card.content.potentialAction = new List<PotentialAction>();
            card.content.potentialAction.Add(potentialAction);

            var contentJson = "";
            log.LogInformation("content json: " + contentJson);
            try
            {
                contentJson = JsonConvert.SerializeObject(card.content, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });


            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                throw;
            }
            var requestUri = "https://outlook.office.com/webhook/cd95bc39-d4b6-47f5-af91-d1bb60673ea5@72f988bf-86f1-41af-91ab-2d7cd011db47/IncomingWebhook/34d5542e15ea4494a397f6081eb1ffbf/ccd20272-10db-42f9-81ff-7ef56c960fc0";
                       
            var content = new StringContent(contentJson);
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = content
            };                       

            var postResponse = await httpClient.PostAsync(requestUri, content);
            var postResponseContent = await postResponse.Content.ReadAsStringAsync();
            var postResponseHeaders = postResponse.Headers;

            return (ActionResult)new OkObjectResult(requestBody);
        }
    }
}
