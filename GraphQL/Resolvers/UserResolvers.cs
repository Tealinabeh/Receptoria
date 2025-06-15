using Receptoria.API.Data;
using Receptoria.API.Models;

namespace Receptoria.API.GraphQL.Resolvers;

[ExtendObjectType(typeof(ReceptoriaUser))]
public class UserResolvers
{
    public int GetRecipeCount([Parent] ReceptoriaUser user, [Service] ApplicationDbContext context)
    {
        return context.Recipes.Count(r => r.AuthorId == user.Id);
    }

    public DateTime? GetRegistrationDate([Parent] ReceptoriaUser user, [Service] ApplicationDbContext context)
    {
        var firstRecipe = context.Recipes
           .Where(r => r.AuthorId == user.Id)
           .OrderBy(r => r.Created)
           .FirstOrDefault();

        return firstRecipe?.Created;
    }

    [GraphQLName("avatarUrl")]
    public string? GetAvatarUrl([Parent] ReceptoriaUser user, [Service] IConfiguration config)
    {
        var publicApiUrl = config["PUBLIC_API_URL"];
        return (user.Avatar != null && user.Avatar.Length > 0)
            ? $"{publicApiUrl}/api/images/avatar/{user.Id}"
            : null;
    }
}