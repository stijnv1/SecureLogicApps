using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

namespace SecureLogicApps
{
    class CreateCredentials
    {
        public static async Task<AzureCredentials> GetCredentials(ExecutionContext context, IConfigurationRoot config)
        {
            try
            {
                AzureCredentials creds;

                string localDevelopment = Environment.GetEnvironmentVariable("LocalDevelopment", EnvironmentVariableTarget.Process);

                if (!string.IsNullOrEmpty(localDevelopment) && localDevelopment.Equals("yes"))
                {
                    // local development environment, use ServicePrincipal for authentication
                    creds = SdkContext.AzureCredentialsFactory
                            .FromServicePrincipal(config["appID"], config["appSecret"], config["tenantID"], AzureEnvironment.AzureGlobalCloud).
                            WithDefaultSubscription(config["subscriptionID"]);
                }
                else
                {
                    // runs in Azure, use MSI
                    creds = SdkContext.AzureCredentialsFactory
                                    .FromMSI(new MSILoginInformation(MSIResourceType.AppService), AzureEnvironment.AzureGlobalCloud);
                }

                return creds;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}