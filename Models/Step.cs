
using System.ComponentModel.DataAnnotations.Schema;

namespace Receptoria.API.Models;

public class Step
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public byte[]? Image { get; set; }
    public int StepNumber { get; set; }
    public Guid RecipeId { get; set; }

    [ForeignKey(nameof(RecipeId))]
    public virtual Recipe Recipe { get; set; } = null!;
}