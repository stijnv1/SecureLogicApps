using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace SecureLogicApps
{
    public static class RotateSASTokenLogicApp
    {
        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("RotateSASTokenLogicApp")]
        public static async Task RunAsync([TimerTrigger("0 */2 * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var config = new ConfigurationBuilder()
                                .SetBasePath(context.FunctionAppDirectory)
                                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                                .AddEnvironmentVariables()
                                .Build();

            string logicAppName = config["logicAppName"];
            string logicAppRGName = config["logicAppRGName"];
            string subscriptionId = config["subscriptionID"];

            try
            {
                AzureCredentials creds = await CreateCredentials.GetCredentials(context, config);

                // create REST client for Azure API calls
                var restClient = RestClient.Configure()
                                    .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                                    .WithCredentials(creds)
                                    .WithBaseUri("https://management.azure.com/")
                                    .Build();

                // construct message for Azure API call to regenerate Logic App SAS token
                var url = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{logicAppRGName}/providers/Microsoft.Logic/workflows/{logicAppName}/regenerateAccessKey?api-version=2016-06-01";
                var HttpMessage = new HttpRequestMessage(HttpMethod.Post, url);
                HttpMessage.Content = new StringContent(JsonSerializer.Serialize(new { keyType = "Primary" }), Encoding.UTF8, "application/json");

                // add authentication header
                await restClient.Credentials.ProcessHttpRequestAsync(HttpMessage, new System.Threading.CancellationToken(false));

                // execute regenerate SAS token
                var httpResponse = await httpClient.SendAsync(HttpMessage);
                var apiResponse = await httpResponse.Content.ReadAsStringAsync();
                log.LogInformation($"primary sas token logic app {logicAppName} regenerated.");
                log.LogInformation($"azure api resonse: {apiResponse}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error occured: subscription used = {subscriptionId}");
            }
        }
    }
}
