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
    /// Retrieve all issues for a specific repository, optionally filtered by labels.
    /// </summary>
    /// <param name="owner">The owner of the repository</param>
    /// <param name="repo">The name of the repository</param>
    /// <param name="labels">The labels to filter by</param>
    /// <param name="includeComments">Flag to include comments</param>
    /// <returns>A list of issues</returns>
    [HttpGet("{owner}/{repo}", Name = "GetAllIssues")]
    [SwaggerOperation(OperationId = "GetAllIssues", Description = "Returns a list of issues associated with a specific GitHub repository.")]    
    [Tags("Issues")]
    [ProducesResponseType(typeof(IEnumerable<Issue>), StatusCodes.Status200OK)]
    [Produces("application/json")]       
    public async Task<IActionResult> GetAllIssues(
        [FromRoute] string owner, 
        [FromRoute] string repo, 
        [FromQuery(Name = "labels")] string[]? labels = null,
        [FromQuery(Name = "comments")] bool includeComments = false)
    {
        var issuesWithComments = await _issuesService.GetIssuesWithCommentsAsync(owner, repo, labels, includeComments);
        return Ok(issuesWithComments);        
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
