using Receptoria.API.Models;

namespace Receptoria.API.GraphQL.Resolvers;

[ExtendObjectType(typeof(Step))]
public class StepResolver
{
    [GraphQLName("imageUrl")]
    public string? GetImageUrl([Parent] Step step)
    {
        return (step.Image != null && step.Image.Length > 0)
            ? $"/api/images/step/{step.Id}"
            : null;
    }
}