namespace Receptoria.API.GraphQL.Requests;

public record UpdateRecipeInput(
    Guid RecipeId,
    string? Title,
    int? Difficulty,
    int? TimeToCook,
    string? Description,
    string[]? Categories,
    string[]? Ingredients,
    IFile? Image,
    ICollection<StepUpdateInput>? Steps,
    bool? RemoveImage = false
);