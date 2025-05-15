using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Elf.Functions.Models;

namespace Elf.Functions.Utilities
{
    public class IssueFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IssueFetcher> _logger;

        public IssueFetcher(
            IHttpClientFactory httpClientFactory, 
            ILogger<IssueFetcher> logger)
        {
            _httpClient = httpClientFactory.CreateClient("ElfApiClient");
            _logger = logger;
        }

        public async Task<List<Issue>> FetchIssuesAsync(ExportIssuesRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Owner) || string.IsNullOrEmpty(request.Repo))
            {
                _logger.LogError("Invalid request. 'Owner' and 'Repo' are required.");
                throw new ArgumentException("Invalid request. 'Owner' and 'Repo' are required.");
            }

            // Build the query string for labels and includeComments
            var queryParameters = new List<string>();
            if (request.Labels != null && request.Labels.Length > 0)
            {
                queryParameters.Add($"labels={string.Join(",", request.Labels)}");
            }
            if (request.Comments)
            {
                queryParameters.Add("comments=true");
            }

            var queryString = queryParameters.Count > 0 ? $"?{string.Join("&", queryParameters)}" : string.Empty;

            var requestUrl = $"issues/{request.Owner}/{request.Repo}{queryString}";
            var response = await _httpClient.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch issues from Elf.Api. Status Code: {StatusCode}", response.StatusCode);
                throw new HttpRequestException($"Failed to fetch issues. Status Code: {response.StatusCode}");
            }

            var issues = await response.Content.ReadFromJsonAsync<List<Issue>>();
            if (issues == null || issues.Count == 0)
            {
                return new List<Issue>(); 
            }

            return issues;
        }

        public async Task<List<Label>> FetchLabelsAsync(string owner, string repo)
        {
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            {
                _logger.LogError("Invalid request. 'Owner' and 'Repo' are required.");
                throw new ArgumentException("Invalid request. 'Owner' and 'Repo' are required.");
            }

            var requestUrl = $"issues/{owner}/{repo}/labels";
            var response = await _httpClient.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch labels from Elf.Api. Status Code: {StatusCode}", response.StatusCode);
                throw new HttpRequestException($"Failed to fetch labels. Status Code: {response.StatusCode}");
            }

            var labels = await response.Content.ReadFromJsonAsync<List<Label>>();
            if (labels == null || labels.Count == 0)
            {
                return new List<Label>(); 
            }

            return labels;
        }

    }
}