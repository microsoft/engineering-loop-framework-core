using Elf.Api.Services;
using Elf.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elf.Api.Controllers;

/// <summary>
/// Controller for managing GitHub issues.
/// </summary>
[ApiController]
[Route("[controller]")]
public class IssuesController : ControllerBase
{
    private readonly GitHubIssuesService _issuesService;

    /// <summary>
    /// Constructor for IssuesController.
    /// </summary>
    /// <param name="issuesService"></param>
    public IssuesController(GitHubIssuesService issuesService)
    {
        _issuesService = issuesService;
    }

    /// <summary>
    /// Create multiple issues in a repository.
    /// </summary>
    /// <param name="owner">The owner of the repository</param>
    /// <param name="repo">The name of the repository</param>
    /// <param name="requests">Array of issue creation requests</param>
    /// <returns>Results for each issue creation attempt</returns>
    [HttpPost("{owner}/{repo}")]
    [SwaggerOperation(OperationId = "CreateIssues", Description = "Creates multiple issues in the specified repository.")]
    [Tags("Issues")]
    [ProducesResponseType(typeof(IEnumerable<CreateIssueResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateIssues(
        [FromRoute] string owner,
        [FromRoute] string repo,
        [FromBody] CreateIssueRequest[] requests)
    {
        if (requests == null || requests.Length == 0)
        {
            return BadRequest(new { Message = "Request body must contain at least one issue to create." });
        }

        var results = await _issuesService.CreateIssuesAsync(owner, repo, requests);
        
        // Add header if we have mixed results (some succeeded, some failed)
        var successCount = results.Count(r => r.Success);
        var totalCount = results.Count;
        
        if (successCount > 0 && successCount < totalCount)
        {
            Response.Headers.Append("X-Partial-Success", $"{successCount}/{totalCount}");
        }

        return Ok(results);
    }

    /// <summary>
    /// Retrieve issues for a specific repository. Optionally filter by labels, include comments, or fetch specific issues by IDs.
    /// </summary>
    /// <param name="owner">The owner of the repository</param>
    /// <param name="repo">The name of the repository</param>
    /// <param name="labels">The labels to filter by</param>
    /// <param name="includeComments">Flag to include comments</param>
    /// <param name="issueIds">Comma-separated list of issue IDs to fetch</param>
    /// <returns>A list of issues</returns>
    [HttpGet("{owner}/{repo}/")]
    [SwaggerOperation(OperationId = "GetIssues", Description = "Returns issues for a repository. Optionally filter by labels, include comments, or fetch specific issues by IDs.")]
    [Tags("Issues")]
    [ProducesResponseType(typeof(IEnumerable<Issue>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<IActionResult> GetIssues(
        [FromRoute] string owner,
        [FromRoute] string repo,
        [FromQuery(Name = "labels")] string[]? labels = null,
        [FromQuery(Name = "comments")] bool includeComments = false,
        [FromQuery(Name = "ids")] string? issueIds = null)
    {
        if (!string.IsNullOrEmpty(issueIds))
        {
            // Parse issue IDs from the query string
            var issueNumbers = issueIds.Split(',').Select(int.Parse).ToList();

            // Fetch specific issues by IDs
            var issues = await _issuesService.GetIssuesByIdsAsync(owner, repo, issueNumbers, includeComments);
            return Ok(issues);
        }

        // Fetch all issues with optional filters
        var issuesWithComments = await _issuesService.GetIssuesWithCommentsAsync(owner, repo, labels, includeComments);
        return Ok(issuesWithComments);
    }

    /// <summary>
    /// Retrieve a single issue by its number.
    /// </summary>
    /// <param name="owner">The owner of the repository</param>
    /// <param name="repo">The name of the repository</param>
    /// <param name="issueNumber">The issue number</param>
    /// <param name="includeComments">Flag to include comments</param>
    /// <returns>The issue</returns>
    [HttpGet("{owner}/{repo}/{issueNumber}")]
    [SwaggerOperation(OperationId = "GetIssueById", Description = "Returns a single issue by its number.")]
    [Tags("Issues")]
    [ProducesResponseType(typeof(Issue), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIssueById(
        [FromRoute] string owner,
        [FromRoute] string repo,
        [FromRoute] int issueNumber,
        [FromQuery(Name = "comments")] bool includeComments = false)
    {
        var issue = await _issuesService.GetIssueByIdAsync(owner, repo, issueNumber, includeComments);
        if (issue == null)
        {
            return NotFound(new { Message = $"Issue #{issueNumber} not found in repository {owner}/{repo}." });
        }
        return Ok(issue);
    }

    /// <summary>
    /// Retrieve all labels for a repository.
    /// </summary>
    /// <param name="owner">The owner of the repository</param>
    /// <param name="repo">The name of the repository</param>
    /// <returns>A list of labels</returns>
    [HttpGet("{owner}/{repo}/labels")]
    [SwaggerOperation(OperationId = "GetAllLabels", Description = "Returns a list of labels associated with a specific GitHub repository.")]    
    [Tags("Labels")]
    [ProducesResponseType(typeof(IEnumerable<Label>), StatusCodes.Status200OK)]
    [Produces("application/json")]    
    public async Task<IActionResult> GetAllLabels(
        [FromRoute] string owner, 
        [FromRoute] string repo)
    {
        var labels = await _issuesService.GetAllLabelsAsync(owner, repo);
        return Ok(labels);    
    }
}
