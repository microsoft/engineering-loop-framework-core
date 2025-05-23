using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Elf.Functions.Models;
using Elf.Functions.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Elf.Functions
{
    public class ExportIssuesToExcel
    {
        private readonly ILogger<ExportIssuesToExcel> _logger;
        private readonly HttpClient _httpClient;
        private readonly IssueFetcher _issueFetcher;
        private readonly BlobServiceClient _blobServiceClient;

        public ExportIssuesToExcel(
            ILogger<ExportIssuesToExcel> logger,
            IHttpClientFactory httpClientFactory,
            IssueFetcher issueFetcher,
            BlobServiceClient blobServiceClient)
        {
            _httpClient = httpClientFactory.CreateClient("ElfApiClient");
            _logger = logger;
            _issueFetcher = issueFetcher;
            _blobServiceClient = blobServiceClient;
        }

        [Function("ExportIssuesToExcel")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("ExportIssuesToExcel function triggered.");

            // Read the request body and validate the input
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<ExportIssuesRequest>(requestBody);                                
            if (request == null || string.IsNullOrEmpty(request.Owner) || string.IsNullOrEmpty(request.Repo))
            {
                _logger.LogError("Invalid request. 'Owner' and 'Repo' are required.");
                return new BadRequestObjectResult("Invalid request. 'Owner' and 'Repo' are required.");
            }

            // Fetch issues
            var issues = await _issueFetcher.FetchIssuesAsync(request);                        
            if (issues == null || issues.Count == 0)
            {
                _logger.LogWarning("No issues found for the specified repository.");
                return new NotFoundObjectResult("No issues found for the specified repository.");
            }

            _logger.LogInformation("Successfully fetched issues from Elf.Api.");

            // Fetch labels
            var labels = await _issueFetcher.FetchLabelsAsync(request.Owner, request.Repo);

            // Create the Excel file
            var workbook = CreateExcelFileAsync(issues, labels);
            try
            {
                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                // Upload the file to Azure Blob Storage
                var containerName = "issues";

                // Check if a filename is provided in the request
                var blobName = !string.IsNullOrEmpty(request.Filename)
                    ? request.Filename
                    : $"issues-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.UploadAsync(stream, overwrite: true);

                // Generate a SAS token for the blob
                var sasUri = GenerateSasToken(blobClient);

                _logger.LogInformation("File uploaded successfully to Blob Storage. SAS URI: {SasUri}", sasUri);

                // Return the SAS URI as the response
                return new OkObjectResult(new { FileUrl = sasUri });                    
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }                
            finally
            {
                workbook.Dispose();
            }
        }

        private XLWorkbook CreateExcelFileAsync(List<Issue> issues, List<Label> labels)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Issues");

            // Header row
            worksheet.Cell(1, 1).Value = "Issue ID";
            worksheet.Cell(1, 2).Value = "Title";
            worksheet.Cell(1, 3).Value = "Description";
            worksheet.Cell(1, 4).Value = "Created At";
            worksheet.Cell(1, 5).Value = "State";
            worksheet.Cell(1, 6).Value = "Labels";
            worksheet.Cell(1, 7).Value = "Created By";
            worksheet.Cell(1, 8).Value = "URL"; 
            worksheet.Cell(1, 9).Value = "Comments";               

            int row = 2;

            for (int i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];

                // Write issue details in the first row for the issue
                worksheet.Cell(row, 1).Value = issue.Number;
                worksheet.Cell(row, 2).Value = issue.Title;
                worksheet.Cell(row, 3).Value = issue.Body;
                worksheet.Cell(row, 4).Value = issue.CreatedAt.ToString("u");
                worksheet.Cell(row, 5).Value = issue.State;
                worksheet.Cell(row, 6).Value = string.Join(", ", issue.Labels ?? []);
                worksheet.Cell(row, 7).Value = issue.Author;
                worksheet.Cell(row, 8).Value = issue.HtmlUrl;                    

                // Handle comments
                if (issue.Comments != null && issue.Comments.Any())
                {
                    foreach (var comment in issue.Comments)
                    {
                        // Write the comment in the current row                            
                        //worksheet.Cell(row, 9).Value = $"{comment.Author}: {comment.Body}";
                        worksheet.Cell(row, 9).Value = $"Author:{comment.Author}\nDate:{comment.CreatedAt.ToString("u")}\nIssue:{issue.Number}\n{comment.Body}";

                        // Move to the next row for the next comment
                        row++;
                    }
                }
                else
                {
                    worksheet.Cell(row, 9).Value = "";
                    row++; 
                }
            }

            // Return the workbook if no labels are provided
            if (labels == null || labels.Count == 0)
            {
                _logger.LogWarning("No labels found for the specified repository.");
                return workbook;
            }

            var labelWorksheet = workbook.Worksheets.Add("Labels");
            labelWorksheet.Cell(1, 1).Value = "Label Name";
            labelWorksheet.Cell(1, 2).Value = "Description";

            int labelRow = 2;
            foreach (var label in labels)
            {
                labelWorksheet.Cell(labelRow, 1).Value = label.Name;
                labelWorksheet.Cell(labelRow, 2).Value = label.Description;
                labelRow++;
            }

            return workbook;
        }

        private Uri GenerateSasToken(BlobClient blobClient)
        {
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b", // "b" for blob
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // SAS token valid for 1 hour
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                return blobClient.GenerateSasUri(sasBuilder);
            }
            else
            {
                // Fallback to manual SAS token generation
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var storageAccountKey = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_KEY");
                if (string.IsNullOrEmpty(storageAccountKey))
                {
                    throw new InvalidOperationException("STORAGE_ACCOUNT_KEY environment variable is not set.");
                }

                var storageSharedKeyCredential = new Azure.Storage.StorageSharedKeyCredential(
                    blobClient.AccountName,
                    storageAccountKey
                );

                var sasToken = sasBuilder.ToSasQueryParameters(storageSharedKeyCredential).ToString();

                return new Uri($"{blobClient.Uri}?{sasToken}");
            }
        }        
    }
}
