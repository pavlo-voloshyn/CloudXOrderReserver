using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace OrderItemsReserver
{
    public class OrderItemsReserver
    {
        private readonly ILogger<OrderItemsReserver> _logger;
        private const string SUCCESS_MESSAGE = "Order file uploaded successfully";

        public OrderItemsReserver(ILogger<OrderItemsReserver> log)
        {
            _logger = log;
        }

        [FunctionName("OrderItemsReserver")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "order" })]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(object), Description = "order", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            string orderId = Guid.NewGuid().ToString();
            _logger.LogInformation($"Start processing {orderId}");

            try
            {
                var reader = new StreamReader(req.Body);
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                var order = reader.ReadToEnd();

                using (CosmosClient client = new(Environment.GetEnvironmentVariable("CosmosDbConnectionString")))
                {
                    var database = client.GetDatabase(Environment.GetEnvironmentVariable("CosmosDb"));
                    var container = database.GetContainer(Environment.GetEnvironmentVariable("Container"));
                    await container.CreateItemAsync(new
                    {
                        id = orderId,
                        Order = order
                    });

                }
                return new OkObjectResult(SUCCESS_MESSAGE);

            } catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}

