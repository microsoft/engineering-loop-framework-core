using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
using Elf.Functions.Utilities;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Register BlobServiceClient
builder.Services.AddSingleton(provider =>
{
    var blobStorageEndpoint = Environment.GetEnvironmentVariable("BLOB_STORAGE_ENDPOINT");
    if (string.IsNullOrEmpty(blobStorageEndpoint))
    {
        throw new InvalidOperationException("BLOB_STORAGE_ENDPOINT environment variable is not set.");
    }

    return new BlobServiceClient(new Uri(blobStorageEndpoint), new DefaultAzureCredential());
});

// Register OpenAIClient
builder.Services.AddSingleton(provider =>
{
    var openAiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");

    if (string.IsNullOrEmpty(openAiEndpoint))
    {
        throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT environment variable is not set.");
    }

    AzureOpenAIClient client = new(
        new Uri(openAiEndpoint),
        new DefaultAzureCredential());    

    return client;
});

// Register the SearchClient
builder.Services.AddSingleton(provider =>
{
    var searchEndpoint = Environment.GetEnvironmentVariable("SEARCH_ENDPOINT");
    var searchIndexName = Environment.GetEnvironmentVariable("SEARCH_INDEX_NAME") ?? "issues-index";

    if (string.IsNullOrEmpty(searchEndpoint))
    {
        throw new InvalidOperationException("SEARCH_ENDPOINT environment variable is not set.");
    }

    return new SearchClient(new Uri(searchEndpoint), searchIndexName, new DefaultAzureCredential());
});

// Register SearchIndexClient
builder.Services.AddSingleton(provider =>
{
    var searchEndpoint = Environment.GetEnvironmentVariable("SEARCH_ENDPOINT");

    if (string.IsNullOrEmpty(searchEndpoint))
    {
        throw new InvalidOperationException("SEARCH_ENDPOINT environment variable is not set.");
    }

    return new SearchIndexClient(new Uri(searchEndpoint), new DefaultAzureCredential());
});

// Register a named HttpClient for Elf.Api
builder.Services.AddHttpClient("ElfApiClient", client =>
{
    var baseAddress = Environment.GetEnvironmentVariable("ELF_API_BASE_URL");
    if (string.IsNullOrEmpty(baseAddress))
    {
        throw new InvalidOperationException("Environment variable 'ELF_API_BASE_URL' is not set.");
    }

    var apiKey = Environment.GetEnvironmentVariable("ELF_API_KEY");
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
    }

    client.BaseAddress = new Uri(baseAddress);
    client.DefaultRequestHeaders.Add("User-Agent", "Elf.ExportFunctions");
});

builder.Services.AddScoped<IssueFetcher>();

builder.ConfigureFunctionsWebApplication();


// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
