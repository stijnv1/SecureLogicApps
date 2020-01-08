using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Configuration;

namespace SecureLogicApps
{
    public static class CallLogicApp
    {
        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("CallLogicApp")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("Trigger Logic App");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //string message = data;

            var config = new ConfigurationBuilder()
                                .SetBasePath(context.FunctionAppDirectory)
                                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                                .AddEnvironmentVariables()
                                .Build();

            string logicAppName = config["logicAppName"];
            string logicAppRGName = config["logicAppRGName"];

            try
            {
                AzureCredentials creds = await CreateCredentials.GetCredentials(context, config);

                // create REST client for Azure API calls
                var restClient = RestClient.Configure()
                                    .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                                    .WithCredentials(creds)
                                    .WithBaseUri("https://management.azure.com/")
                                    .Build();
                
                // construct message for Azure API call to retrieve Logic App HTTP Trigger URL
                var url = $"https://management.azure.com/subscriptions/{creds.DefaultSubscriptionId}/resourceGroups/{logicAppRGName}/providers/Microsoft.Logic/workflows/{logicAppName}/triggers/manual/listCallbackUrl?api-version=2016-06-01";
                var HttpMessage = new HttpRequestMessage(HttpMethod.Post, url);

                // add authentication header
                await restClient.Credentials.ProcessHttpRequestAsync(HttpMessage, new System.Threading.CancellationToken(false));

                // retrieve logic app url
                var httpResponse = await httpClient.SendAsync(HttpMessage);
                var apiResponse = await httpResponse.Content.ReadAsStringAsync();

                // call Logic App HTTP Trigger to initiate the Logic App
                var authenticatedUrl = (string)JsonConvert.DeserializeObject<dynamic>(apiResponse).value;
                await httpClient.PostAsync(authenticatedUrl, new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json"));

                return (ActionResult)new OkObjectResult($"Logic App {logicAppName} called");

            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error occured");
                return new BadRequestObjectResult(String.Format("Error occured in function: {0}", ex.Message));
            }
        }
    }
}
