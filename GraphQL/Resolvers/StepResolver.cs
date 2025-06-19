using Receptoria.API.Models;

namespace Receptoria.API.GraphQL.Resolvers;

[ExtendObjectType(typeof(Step))]
public class StepResolver
{
    [GraphQLName("imageUrl")]
    public string? GetImageUrl(
        [Parent] Step step,
        [Service] IConfiguration config,
        int? width,
        int? height)
    {
        var publicApiUrl = config["PUBLIC_API_URL"];
        if (step.Image == null || step.Image.Length == 0) return null;

        var url = $"{publicApiUrl}/api/images/step/{step.Id}";

        var queryParams = new List<string>();
        if (width.HasValue) queryParams.Add($"w={width.Value}");
        if (height.HasValue) queryParams.Add($"h={height.Value}");

        return queryParams.Any() ? $"{url}?{string.Join("&", queryParams)}" : url;
    }
}
