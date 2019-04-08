using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace execute_azure_function_with_triggers
{
    public static class BlobTrigger1
    {
        [FunctionName("BlobTrigger1")]
        public static void Run([BlobTrigger("container/{name}", Connection = "BlobConnectionString")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }
    }
}
