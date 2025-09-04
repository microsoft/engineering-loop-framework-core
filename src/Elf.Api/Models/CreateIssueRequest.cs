namespace Elf.Api.Models;

/// <summary>
/// Create issue request
/// </summary>
public class CreateIssueRequest
{
    /// <summary>
    /// TItle of the issue
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Body of the issue
    /// </summary>
    public required string Body { get; set; }

    /// <summary>
    /// Labels for the issue
    /// </summary>
    public string[]? Labels { get; set; }

    /// <summary>
    /// Assignees
    /// </summary>
    public string[]? Assignees { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public int? Milestone { get; set; }
}
