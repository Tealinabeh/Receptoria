namespace Receptoria.API.GraphQL.Requests;

public record CreateRecipeInput(
    string Title,
    string Description,
    int Difficulty,
    int TimeToCook,
    string[] Categories,
    string[] Ingredients,
    IFile? Image,
    ICollection<StepInput> Steps
);