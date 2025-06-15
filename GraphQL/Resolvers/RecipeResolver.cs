using Microsoft.AspNetCore.Identity;
using Receptoria.API.Models;

namespace Receptoria.API.GraphQL;

[ExtendObjectType(typeof(Recipe))]
public class RecipeResolver
{
    [GraphQLName("imageUrl")]
    public string? GetImageUrl([Parent] Recipe recipe, [Service] IConfiguration config)
    {
        var publicApiUrl = config["PUBLIC_API_URL"];
        return (recipe.Image != null && recipe.Image.Length > 0)
            ? $"{publicApiUrl}/api/images/recipe/{recipe.Id}"
            : null;
    }

    public async Task<ReceptoriaUser?> GetAuthor(
        [Parent] Recipe recipe,
        [Service] UserManager<ReceptoriaUser> userManager)
    {
        return await userManager.FindByIdAsync(recipe.AuthorId);
    }

    [GraphQLName("categories")]
    public IEnumerable<string> GetFormattedCategories([Parent] Recipe recipe)
    {
        if (recipe.Categories is null || !recipe.Categories.Any())
        {
            return Enumerable.Empty<string>();
        }
        return recipe.Categories.Select(c =>
        {
            if (string.IsNullOrEmpty(c)) return string.Empty;
            return char.ToUpper(c[0]) + c.Substring(1);
        });
    }
}