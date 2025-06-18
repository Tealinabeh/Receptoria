namespace Receptoria.API.GraphQL.Requests;

public record UpdateRecipeInput(
    Guid RecipeId,
    string? Title,
    int? Difficulty,
    int? TimeToCook,
    string? Description,
    string[]? Categories,
    string[]? Ingredients,
    IFile? MainImage,
    ICollection<StepUpdateInput>? Steps,
    bool? RemoveMainImage = false
);