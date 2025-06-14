using System.Security.Claims;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Receptoria.API.Data;
using Receptoria.API.Extensions;
using Receptoria.API.GraphQL.Payloads;
using Receptoria.API.GraphQL.Requests;
using Receptoria.API.Models;

namespace Receptoria.API.GraphQL;

[Authorize]
[ExtendObjectType(typeof(Mutation))]
public class RecipeMutation
{
    public async Task<Recipe> CreateRecipeAsync(
        CreateRecipeInput input,
        [Service] ApplicationDbContext context,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken token)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var mainImageBytes = await input.Image.ToByteArrayAsync(token);

        var recipe = new Recipe
        {
            Title = input.Title,
            Description = input.Description,
            Difficulty = input.Difficulty,
            TimeToCook = input.TimeToCook,
            Categories = input.Categories.Select(c => c.Trim().ToLower()).ToArray(),
            Ingredients = input.Ingredients,
            IngredientCount = input.Ingredients.Length,
            Image = mainImageBytes,
            AuthorId = userId,
            Created = DateTime.UtcNow,
            Steps = new List<Step>()
        };

        if (input.Steps != null && input.Steps.Any())
        {
            int currentStepNumber = 1;
            foreach (var stepInput in input.Steps)
            {
                var stepImageBytes = await stepInput.Image.ToByteArrayAsync(token);
                recipe.Steps.Add(new Step
                {
                    Description = stepInput.Description,
                    Image = stepImageBytes,
                    StepNumber = currentStepNumber++
                });
            }
        }

        context.Recipes.Add(recipe);
        await context.SaveChangesAsync(token);

        await context
                .Entry(recipe)
                .Reference(r => r.Author)
                .LoadAsync(token);

        return recipe;
    }

    public async Task<Recipe> UpdateRecipeAsync(
        UpdateRecipeInput input,
        [Service] ApplicationDbContext context,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken token)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var existingRecipe = await context.Recipes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == input.RecipeId, token);

        if (existingRecipe is null)
        {
            throw new GraphQLException(new Error("Recipe not found.", "RECIPE_NOT_FOUND"));
        }

        if (existingRecipe.AuthorId != userId)
        {
            throw new GraphQLException(new Error("You are not authorized to edit this recipe.", "ACCESS_DENIED"));
        }
        var oldSteps = await context.Steps.Where(s => s.RecipeId == input.RecipeId).ToListAsync(token);
        if (oldSteps.Any())
        {
            context.Steps.RemoveRange(oldSteps);
            await context.SaveChangesAsync(token);
        }
        var recipeToUpdate = await context.Recipes.FirstAsync(r => r.Id == input.RecipeId, token);

        recipeToUpdate.Title = input.Title ?? recipeToUpdate.Title;
        recipeToUpdate.Description = input.Description ?? recipeToUpdate.Description;
        recipeToUpdate.Difficulty = input.Difficulty ?? recipeToUpdate.Difficulty;
        recipeToUpdate.TimeToCook = input.TimeToCook ?? recipeToUpdate.TimeToCook;


        if (input.Categories is not null)
        {
            recipeToUpdate.Categories = input.Categories.Select(c => c.Trim().ToLower()).ToArray();
        }
        if (input.Ingredients is not null)
        {
            recipeToUpdate.Ingredients = input.Ingredients;
            recipeToUpdate.IngredientCount = input.Ingredients.Length;
        }

        if (input.Image is not null)
        {
            recipeToUpdate.Image = await input.Image.ToByteArrayAsync(token);
        }
        
        if (input.Steps is not null && input.Steps.Any())
        {
            int currentStepNumber = 1;
            foreach (var stepInput in input.Steps)
            {
                context.Steps.Add(new Step
                {
                    RecipeId = recipeToUpdate.Id,
                    Description = stepInput.Description,
                    Image = stepInput.Image is not null ? await stepInput.Image.ToByteArrayAsync(token) : null,
                    StepNumber = currentStepNumber++
                });
            }
        }

        await context.SaveChangesAsync(token);
        var result = await context.Recipes
            .AsNoTracking().Include(r => r.Author).Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == recipeToUpdate.Id, token);
        
        return result!;
    }


    public async Task<DeleteRecipePayload> DeleteRecipeAsync(
    Guid recipeId,
    [Service] ApplicationDbContext context,
    [Service] IHttpContextAccessor httpContextAccessor,
    CancellationToken token)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);

        var recipe = await context.Recipes
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == recipeId, token);

        if (recipe is null)
        {
            return new DeleteRecipePayload(true, "Recipe not found or already deleted.");
        }
        if (recipe.AuthorId != userId)
        {
            throw new GraphQLException(new Error("You are not authorized to delete this recipe.", "ACCESS_DENIED"));
        }
        if (recipe.Steps.Any())
        {
            context.Steps.RemoveRange(recipe.Steps);
        }
        var ratings = await context.Ratings.Where(r => r.RecipeId == recipeId).ToListAsync(token);
        if (ratings.Any())
        {
            context.Ratings.RemoveRange(ratings);
        }

        context.Recipes.Remove(recipe);
        await context.SaveChangesAsync(token);

        return new DeleteRecipePayload(true, "Recipe successfully deleted.");
    }
}