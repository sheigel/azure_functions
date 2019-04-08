using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace azure_functions
{
    public static class fan_in_fan_out
    {
        [FunctionName("E2_BackupSiteContent")]
        public static async Task<long> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext backupContext)
        {
            string rootDirectory = Environment.ExpandEnvironmentVariables(backupContext.GetInput<string>() ?? "");
            if (string.IsNullOrEmpty(rootDirectory))
            {
                throw new ArgumentException("A directory path is required as an input.");
            }

            if (!Directory.Exists(rootDirectory))
            {
                throw new DirectoryNotFoundException($"Could not find a directory named '{rootDirectory}'.");
            }

            string[] files = await backupContext.CallActivityAsync<string[]>(
                "E2_GetFileList",
                rootDirectory);

            var tasks = new Task<long>[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                tasks[i] = backupContext.CallActivityAsync<long>(
                    "E2_CopyFileToBlob",
                    files[i]);
            }

            await Task.WhenAll(tasks);

            long totalBytes = tasks.Sum(t => t.Result);
            return totalBytes;
        }

        [FunctionName("E2_GetFileList")]
        public static string[] E2_GetFileList([ActivityTrigger] string rootDirectory, ILogger log)
        {
            string[] files = Directory.GetFiles(rootDirectory, "*", SearchOption.AllDirectories);
            log.LogInformation($"Found {files.Length} file(s) under {rootDirectory}.");

            return files;
        }

        [FunctionName("E2_CopyFileToBlob")]

        public static long E2_CopyFileToBlob([ActivityTrigger] string filePath, Binder binder, ILogger log)
        {
            long byteCount = new FileInfo(filePath).Length;

            // strip the drive letter prefix and convert to forward slashes
            string blobPath = filePath
                .Substring(Path.GetPathRoot(filePath).Length)
                .Replace('\\', '/');
            string outputLocation = $"backups/{blobPath}";

            log.LogInformation($"Copying '{filePath}' to '{outputLocation}'. Total bytes = {byteCount}.");


            return byteCount;
        }
        [FunctionName("durable_fan_in_fan_out")]
        public static async Task<HttpResponseMessage> HttpStart(
                    [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequestMessage req,
                    [OrchestrationClient]DurableOrchestrationClient starter,
                    ILogger log)
        {
            var folderPath=(await req.Content.ReadAsAsync<FolderInfo>()).folder_path;
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("E2_BackupSiteContent", folderPath);

            log.LogInformation($"Started orchestration with ID = '{instanceId}' for path = '{folderPath}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }

    public class FolderInfo{
        public string folder_path;
    }
}