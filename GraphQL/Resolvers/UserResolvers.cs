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

    [GraphQLName("avatarUrl")]
    public string? GetAvatarUrl(
        [Parent] ReceptoriaUser user,
        [Service] IConfiguration config,
        int? width,
        int? height)
    {
        var publicApiUrl = config["PUBLIC_API_URL"];
        if (user.Avatar == null || user.Avatar.Length == 0) return null;

        var url = $"{publicApiUrl}/api/images/avatar/{user.Id}";

        var queryParams = new List<string>();
        if (width.HasValue) queryParams.Add($"w={width.Value}");
        if (height.HasValue) queryParams.Add($"h={height.Value}");

        return queryParams.Any() ? $"{url}?{string.Join("&", queryParams)}" : url;
    }
}