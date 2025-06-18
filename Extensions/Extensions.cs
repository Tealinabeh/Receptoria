using Receptoria.API.GraphQL.Requests;
using Receptoria.API.Models;

namespace Receptoria.API.Extensions;

public static class Extensions
{
    public static async Task<byte[]?> ToByteArrayAsync(this IFile? file, CancellationToken token = default)
    {
        if (file is null) return null;
        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, token);
        return memoryStream.ToArray();
    }
    public static async Task<Recipe> ToRecipeAsync(this CreateRecipeInput input, string userId, CancellationToken token = default)
    {
        var mainImageBytes = await input.Image.ToByteArrayAsync(token);

        var recipe = new Recipe
        {
            Title = input.Title,
            Description = input.Description,
            Difficulty = input.Difficulty,
            TimeToCook = input.TimeToCook,
            Categories = input.Categories,
            Ingredients = input.Ingredients,
            MainImage = mainImageBytes,
            AuthorId = userId,
            Created = DateTime.UtcNow,
            Steps = new List<Step>()
        };

        foreach (var stepInput in input.Steps)
        {
            var stepImageBytes = await stepInput.Image.ToByteArrayAsync(token);
            var newStep = new Step
            {
                Description = stepInput.Description,
                Image = stepImageBytes
            };
            recipe.Steps.Add(newStep);
        }

        return recipe;
    }

    public static async Task ApplyChangesExceptSteps(this Recipe recipe, UpdateRecipeInput input, CancellationToken token = default)
    {
        if (input.Title is not null) recipe.Title = input.Title;
        if (input.Description is not null) recipe.Description = input.Description;
        if (input.Difficulty.HasValue) recipe.Difficulty = input.Difficulty.Value;
        if (input.TimeToCook.HasValue) recipe.TimeToCook = input.TimeToCook.Value;
        if (input.Categories is not null) recipe.Categories = input.Categories;
        if (input.Ingredients is not null) recipe.Ingredients = input.Ingredients;
        if (input.MainImage is not null) recipe.MainImage = await input.MainImage.ToByteArrayAsync(token);
    }
}