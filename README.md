# Secure HTTP-Triggered Logic Apps
Based on this blog post: https://www.re-mark-able.net/secure-logic-app-htpp-triggers/

HTTP-Triggered Function to retrieve Logic App URL and call the logic app, passing the required JSON body for the logic app.
Time-Triggered Function to regenerate the HTTP-Triggered Logic App SAS token every x minutes/hours/days

Following configuration settings are required:
- subscriptionID : subscription id of azure subscription containing logic app
- logicAppRGName : resource group containing logic app
- logicAppName: name of logic app

For local development, following local settings can be used:
```
{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "appID": "app id of SPN with logic app contributor permissions on the logic app",
    "appSecret": "secret of the used SPN",
    "tenantID": "your tenant id",
    "subscriptionID": "your azure subscription id",
    "logicAppRGName": "logic app resource group name",
    "logicAppName": "logic app name",
    "LocalDevelopment": "yes"
  }
}
```