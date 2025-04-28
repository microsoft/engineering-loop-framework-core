using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Elf.Functions
{
    public class ExportIssuesToExcel
    {
        private readonly ILogger<ExportIssuesToExcel> _logger;

        public ExportIssuesToExcel(ILogger<ExportIssuesToExcel> logger)
        {
            _logger = logger;
        }

        [Function("ExportIssuesToExcel")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
