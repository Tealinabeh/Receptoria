using System.ComponentModel.DataAnnotations.Schema;

namespace Receptoria.API.Models;

public class Rating
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Score { get; set; }
    public Guid RecipeId { get; set; }
    [ForeignKey(nameof(RecipeId))]
    public virtual Recipe Recipe { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    [ForeignKey(nameof(UserId))]
    public virtual ReceptoriaUser User { get; set; } = null!;
}
