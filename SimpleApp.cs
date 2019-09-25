using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Web.Http;
using System;

namespace Functions
{
    public static class SimpleApp
    {
        [FunctionName("SimpleApp")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("SimpleApp_Hello", "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>("SimpleApp_Hello", "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>("SimpleApp_Hello", "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("SimpleApp_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("SimpleApp_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("SimpleApp", null);

            for(int i = 0; i <= 10; i++)
            {
                log.LogWarning($"Counter=>{i}");
                var status = await starter.GetStatusAsync(instanceId);
                if(status?.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
                {
                    var resp = new HttpResponseMessage();
                    resp.Content = new StringContent(status.Output.ToString(), System.Text.Encoding.UTF8, "application/json");

                    return resp;                     
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
            response.Content = new StringContent("Function has timed out", System.Text.Encoding.UTF8, "application/json");

            return response; 
        }
    }
}