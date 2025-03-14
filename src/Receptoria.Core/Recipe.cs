namespace Receptoria.Core;

public record Recipe
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int Rating { get; set; }
    public string Description { get; set; } = default!;
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Ingredient> Ingredients { get; set; } = [];
    public ICollection<string> Steps { get; set; } = [];
    public string Summary { get; set; } = default!;
}