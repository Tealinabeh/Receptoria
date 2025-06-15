using Receptoria.API.Models;

namespace Receptoria.API.GraphQL.Resolvers;

[ExtendObjectType(typeof(Step))]
public class StepResolver
{
    [GraphQLName("imageUrl")]
    public string? GetImageUrl([Parent] Step step, [Service] IConfiguration config)
    {
        var publicApiUrl = config["PUBLIC_API_URL"];
        return (step.Image != null && step.Image.Length > 0)
            ? $"{publicApiUrl}/api/images/step/{step.Id}"
            : null;
    }
}