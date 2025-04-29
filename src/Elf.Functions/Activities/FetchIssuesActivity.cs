using Elf.Functions.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Elf.Functions.Models;

namespace Elf.Functions.Activities
{
    public class FetchIssuesActivity
    {
        private readonly IssueFetcher _issueFetcher;

        public FetchIssuesActivity(IssueFetcher issueFetcher)
        {
            _issueFetcher = issueFetcher;
        }

        [Function(nameof(FetchIssuesActivity))]
        public async Task<List<Issue>> Run(
            [ActivityTrigger] ExportIssuesRequest request, 
            FunctionContext context)
        {
            var logger = context.GetLogger<FetchIssuesActivity>();
            logger.LogInformation("Fetching issues from ELF API for repository {Owner}/{Repo}.", request.Owner, request.Repo);

            try
            {
                return await _issueFetcher.FetchIssuesAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch issues from ELF API.");
                throw;
            }
        }
    }
}