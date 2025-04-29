using Azure;
using System.Text;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Elf.Functions.Models;
using OpenAI.Embeddings;
using Azure.Identity;

namespace Elf.Functions.Activities
{
    public class CreateAISearchDocumentActivity
    {
        private readonly SearchClient _searchClient;
        private readonly SearchIndexClient _searchIndexClient;
        private ILogger<CreateAISearchDocumentActivity> _logger;
        private AzureOpenAIClient _openAiClient;

        public CreateAISearchDocumentActivity(
            SearchClient searchClient, 
            SearchIndexClient searchIndexClient, 
            AzureOpenAIClient openAiClient,
            ILogger<CreateAISearchDocumentActivity> logger)
        {
            _searchClient = searchClient;
            _searchIndexClient = searchIndexClient;
            _openAiClient = openAiClient;
            _logger = logger;
        }

        [Function(nameof(CreateAISearchDocumentActivity))]
        public async Task Run([ActivityTrigger] Issue issue, FunctionContext context)
        {            
            _logger.LogInformation("Creating AI Search document for issue #{IssueNumber}.", issue.Number);

            try
            {
                var embedding = await GenerateIssueEmbeddingAsync(issue);

                var test = embedding.ToFloats();

                var document = new
                {
                    Id = issue.Number.ToString(),
                    Title = issue.Title,
                    Body = issue.Body,
                    Labels = issue.Labels,
                    Embedding = embedding.ToFloats(),
                    CreatedDate = issue.CreatedAt,
                    UpdatedDate = issue.UpdatedAt,
                    State = issue.State,
                    //Assignee = issue.Assignee,
                    Url = issue.HtmlUrl
                };

                // Upload the document to the search index
                var response = await _searchClient.UploadDocumentsAsync(new[] { document });
                if (response.GetRawResponse().Status != 200)
                {
                    _logger.LogError("Failed to upload document for issue #{IssueNumber}.", issue.Number);
                    throw new Exception($"Failed to upload document: {response.GetRawResponse().ReasonPhrase}");
                }        

                _logger.LogInformation("Document for issue #{IssueNumber} created successfully.", issue.Number);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create AI Search document for issue #{IssueNumber}.", issue.Number);
                throw;
            }
        }

        public async Task<OpenAIEmbedding> GenerateIssueEmbeddingAsync(Issue issue)
        {
            if (issue == null || string.IsNullOrWhiteSpace(issue.Title))
                throw new ArgumentException("Issue or Issue Title cannot be null.");

            // Get the model name from environment variables
            var modelName = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_MODEL");
            if (string.IsNullOrWhiteSpace(modelName))            
                throw new InvalidOperationException("Model name cannot be null or empty.");

            // Format the issue for embedding
            string inputText = FormatIssueForEmbedding(issue);                  
            
        
            EmbeddingClient embeddingClient = _openAiClient.GetEmbeddingClient(modelName);
            var embeddingOptions = new EmbeddingGenerationOptions();
            var embedding = await embeddingClient.GenerateEmbeddingAsync(inputText, embeddingOptions);
            
            return embedding;
        }

        private string FormatIssueForEmbedding(Issue issue)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Issue Title: {issue.Title}");
            sb.AppendLine();
            sb.AppendLine($"Issue Body: {issue.Body}");  
            sb.AppendLine();
            sb.AppendLine($"Labels: {string.Join(", ", issue.Labels ?? new List<string>())}");

            return sb.ToString();
        }
    }
}