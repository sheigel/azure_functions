using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace execute_azure_function_with_triggers
{
    public static class TimeTrigger
    {
        [FunctionName("TimeTrigger1")]
        public static void Run([TimerTrigger("20 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
