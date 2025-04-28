namespace Elf.Api.Models;

/// <summary>
/// Represents a GitHub issue.
/// </summary>
public class Issue
{
        /// <summary>
        /// The unique identifier for the issue
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// The author of the issue
        /// </summary>
        public required string Author { get; set; }

        /// <summary>
        /// The title of the issue
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// The body of the issue
        /// </summary>
        public required string Body { get; set; }

        /// <summary>
        /// The state of the issue (e.g., open, closed)
        /// </summary>
        public required string State { get; set; }

        /// <summary>
        /// The labels associated with the issue
        /// </summary>
        public List<string> Labels { get; set; } = new();

        /// <summary>
        /// The date and time when the issue was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The date and time when the issue was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// The URL to the issue on GitHub
        /// </summary>
        public required string HtmlUrl { get; set; }

        /// <summary>
        /// The comments associated with the issue
        /// </summary>
        public List<Comment> Comments { get; set; } = new();
}