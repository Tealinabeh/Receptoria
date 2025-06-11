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
    public string? GetAvatarUrl([Parent] ReceptoriaUser user)
    {
        if (user.Avatar != null && user.Avatar.Length > 0)
        {
            return $"/api/images/avatar/{user.Id}";
        }

        return null;
    }
}