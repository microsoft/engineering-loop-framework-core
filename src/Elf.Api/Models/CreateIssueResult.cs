namespace Elf.Api.Models
{
    /// <summary>
    /// Result from create issue request
    /// </summary>
    public class CreateIssueResult
    {
        /// <summary>
        /// Issue title
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// Issue created
        /// </summary>
        public Issue? Issue { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Success result
        /// </summary>
        public bool Success => Issue != null && Error == null;
    }
}