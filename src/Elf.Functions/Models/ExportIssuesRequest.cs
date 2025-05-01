using System.Text.Json.Serialization;

namespace Elf.Functions.Models
{
    public class ExportIssuesRequest
    {
        [JsonPropertyName("owner")]
        public string Owner { get; set; } = string.Empty;
        
        [JsonPropertyName("repo")]
        public string Repo { get; set; } = string.Empty;
        
        [JsonPropertyName("labels")]
        public string[]? Labels { get; set; }
        
        [JsonPropertyName("comments")]
        public bool Comments { get; set; } = false;

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;
    }
}