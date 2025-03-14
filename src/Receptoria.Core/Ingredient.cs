namespace Receptoria.Core;

public record Ingredient
{
    public string Name { get; set; } = default!;
    public string Amount { get; set; } = default!;
}
