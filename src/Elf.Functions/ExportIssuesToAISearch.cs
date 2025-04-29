using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using Elf.Functions.Orchestrators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Elf.Functions.Models;

namespace Elf.Functions
{
    public class ExportIssuesToAISearch
    {
        private readonly ILogger<ExportIssuesToExcel> _logger;
        private readonly HttpClient _httpClient;

        public ExportIssuesToAISearch(ILogger<ExportIssuesToExcel> logger,
            IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ElfApiClient");
            _logger = logger;
        }     

        [Function("ExportIssuesToAISearch_HttpStart")]
        public async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("ExportIssuesToAISearch_HttpStart");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<ExportIssuesRequest>(requestBody);                
            if (request == null || string.IsNullOrEmpty(request.Owner) || string.IsNullOrEmpty(request.Repo))
            {
                _logger.LogError("Invalid request. 'Owner' and 'Repo' are required.");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badRequestResponse.WriteString("Invalid request. 'Owner' and 'Repo' are required.");
                return badRequestResponse;
            }
            
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(ExportIssuesOrchestrator), request);

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }        
    }
}