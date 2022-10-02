using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace OrderItemsReserver
{
    public class UploadOrderFile
    {
        private readonly ILogger<UploadOrderFile> _logger;
        private const string SUCCESS_MESSAGE = "Order file uploaded successfully";

        public UploadOrderFile(ILogger<UploadOrderFile> log)
        {
            _logger = log;
        }

        [FunctionName("UploadOrderFile")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "order" })]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(object), Description = "order", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            string orderBlobName = $"{Guid.NewGuid()}.json";
            _logger.LogInformation($"Start processing {orderBlobName}");

            try
            {
                string Connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                string containerName = Environment.GetEnvironmentVariable("ContainerName");

                var blobClient = new BlobContainerClient(Connection, containerName);
                var orderBlobClient = blobClient.GetBlobClient(orderBlobName);
                await orderBlobClient.UploadAsync(req.Body);

                return new OkObjectResult(SUCCESS_MESSAGE);

            } catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}

