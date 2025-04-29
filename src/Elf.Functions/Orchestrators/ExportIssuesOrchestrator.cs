using Elf.Functions.Activities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Elf.Functions.Models;

namespace Elf.Functions.Orchestrators
{
    public static class ExportIssuesOrchestrator
    {
        [Function(nameof(ExportIssuesOrchestrator))]
        public static async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context, 
            ExportIssuesRequest request)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(ExportIssuesOrchestrator));

            logger.LogInformation("ExportIssuesOrchestrator started.");

            // Fetch issues from the ELF API
            var issues = await context.CallActivityAsync<List<Issue>>(nameof(FetchIssuesActivity), request);

            // Ensure the index exists (call once before processing documents)
            await context.CallActivityAsync(nameof(EnsureIndexExistsActivity));

            // Process each issue
            foreach (var issue in issues)
            {
                await context.CallActivityAsync(nameof(CreateAISearchDocumentActivity), issue);
            }

            logger.LogInformation("ExportIssuesOrchestrator completed.");

        }    
    }
}