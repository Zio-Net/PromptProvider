using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PromptProvider.Models;

[Table("Prompts")]
public record PromptModel
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(120)]
    public string PromptKey { get; set; } = null!;

    // Auto-generated immutable timestamp string
    [Required, MaxLength(50)]
    public string Version { get; init; } = null!;

    [Required]
    public string Content { get; set; } = null!;
}