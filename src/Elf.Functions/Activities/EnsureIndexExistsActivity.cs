using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
namespace Elf.Functions.Activities
{
    public class EnsureIndexExistsActivity
    {
        private readonly SearchClient _searchClient;
        private readonly SearchIndexClient _searchIndexClient;
        private ILogger<EnsureIndexExistsActivity> _logger;

        public EnsureIndexExistsActivity(
            SearchClient searchClient, 
            SearchIndexClient searchIndexClient, 
            ILogger<EnsureIndexExistsActivity> logger)
        {
            _searchClient = searchClient;
            _searchIndexClient = searchIndexClient;
            _logger = logger;
        }

        [Function(nameof(EnsureIndexExistsActivity))]
        public async Task Run([ActivityTrigger] object input)
        {
            var indexName = _searchClient.IndexName;
            bool indexExists = false;
            await foreach (var index in _searchIndexClient.GetIndexNamesAsync())
            {
                if (index.Equals(indexName, StringComparison.OrdinalIgnoreCase))
                {
                    indexExists = true;
                    break;
                }
            }
            
            if (!indexExists)
            {
                await SetupIndexAsync(indexName);
            }            
        }

        internal async Task SetupIndexAsync(string indexName)
        {
            const string vectorSearchHnswProfile = "my-vector-profile";
            const string vectorSearchHnswConfig = "myHnsw";
            //const string vectorSearchVectorizer = "myOpenAIVectorizer";
            const string semanticSearchConfig = "my-semantic-config";

            SearchIndex searchIndex = new(indexName)
            {
                VectorSearch = new()
                {
                    Profiles =
                    {
                        new VectorSearchProfile(vectorSearchHnswProfile, vectorSearchHnswConfig)
                        // new VectorSearchProfile(vectorSearchHnswProfile, vectorSearchHnswConfig)
                        // {
                        //     VectorizerName = vectorSearchVectorizer
                        // }
                    },
                    Algorithms =
                    {
                        new HnswAlgorithmConfiguration(vectorSearchHnswConfig)
                    }
                },
                SemanticSearch = new()
                {
                    Configurations =
                        {
                           new SemanticConfiguration(semanticSearchConfig, new()
                           {
                                TitleField = new SemanticField("title"),
                                ContentFields =
                                {
                                    new SemanticField("body")
                                },
                                KeywordsFields =
                                {
                                    new SemanticField("labels")
                                }
                           })

                    },
                },
                Fields =
                {
                    new Azure.Search.Documents.Indexes.Models.SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new SearchableField("title") { IsFilterable = true, IsSortable = true},
                    new SearchableField("body") { IsFilterable = true },
                    new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = 1536,
                        VectorSearchProfileName = vectorSearchHnswProfile
                    },
                    new SearchField("labels", SearchFieldDataType.Collection(SearchFieldDataType.String)){IsSearchable = true,IsFilterable = true,IsFacetable = true},
                    new SearchField("source", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                    new SearchField("createdDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                    new SearchField("updatedDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                    new SearchField("state", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                    new SearchField("assignee", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                    new SearchField("url", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true }                 
                }
            };

            try
            {
                await _searchIndexClient.CreateOrUpdateIndexAsync(searchIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex, "Failed to create or update index {IndexName}.", indexName);
                throw;
            }

        }

    }
}