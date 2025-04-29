
namespace Elf.Functions.Models
{
    /// <summary>
    /// Represents a comment on a GitHub issue.
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// The author of the comment
        /// </summary>
        public required string Author { get; set; }
        
        /// <summary>
        /// The body of the comment
        /// </summary>
        public required string Body { get; set; }
        
        /// <summary>
        /// The date and time when the comment was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}