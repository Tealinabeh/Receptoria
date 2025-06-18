using System.ComponentModel.DataAnnotations.Schema;

namespace Receptoria.API.Models;

public class Recipe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public byte[]? Image { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Difficulty { get; set; } // 1 to 3
    public int TimeToCook { get; set; } // in minutes
    public string Description { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public string AuthorId { get; set; } = string.Empty;
    [ForeignKey(nameof(AuthorId))]
    public virtual ReceptoriaUser Author { get; set; } = null!;
    public string[] Categories { get; set; } = Array.Empty<string>();
    public string[] Ingredients { get; set; } = Array.Empty<string>();
    public int IngredientCount { get; set; }
    public virtual ICollection<Step> Steps { get; set; } = new List<Step>();
    public float AverageRating { get; set; } // 1 to 5
}