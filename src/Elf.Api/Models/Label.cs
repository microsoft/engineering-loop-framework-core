namespace Elf.Api.Models;

/// <summary>
/// Represents a label in a GitHub repository.
/// </summary>
public class Label
{
        /// <summary>
        /// Name of the label
        /// </summary>
        public required string Name { get; set; }        

        /// <summary>
        /// Description of the label
        /// </summary>
        public required string Description { get; set; }
}