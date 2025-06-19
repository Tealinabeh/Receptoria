using System.Security.Claims;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using Receptoria.API.Data;
using Receptoria.API.Models;
using Receptoria.API.Services;

namespace Receptoria.API.GraphQL;

[Authorize]
[ExtendObjectType(typeof(Mutation))]
public class RatingMutation
{
    public async Task<Recipe> RateRecipeAsync(
        Guid recipeId,
        int score,
        [Service] ApplicationDbContext context,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ICacheService cacheService,
        CancellationToken token)
    {
        if (score < 1 || score > 5)
        {
            throw new GraphQLException(new Error("Rating score must be between 1 and 5.", "INVALID_SCORE"));
        }

        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var recipe = await context.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId, token);

        if (recipe is null)
        {
            throw new GraphQLException(new Error("Recipe not found.", "RECIPE_NOT_FOUND"));
        }
        if (recipe.AuthorId == userId)
        {
            throw new GraphQLException(new Error("You cannot rate your own recipe.", "SELF_RATING_NOT_ALLOWED"));
        }

        var existingRating = await context.Ratings
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId && r.UserId == userId, token);

        if (existingRating is null)
        {
            var newRating = new Rating { RecipeId = recipeId, UserId = userId!, Score = score };
            context.Ratings.Add(newRating);
        }
        else
        {
            existingRating.Score = score;
        }

        await context.SaveChangesAsync(token);

        var newAverage = await context.Ratings
            .Where(r => r.RecipeId == recipeId)
            .AverageAsync(r => r.Score, cancellationToken: token);

        recipe.AverageRating = (float)Math.Round(newAverage, 2);

        await context.SaveChangesAsync(token);

        string cacheKey = $"Recipe-{recipeId}";
        await cacheService.RemoveAsync(cacheKey, token);

        return recipe;
    }

    public async Task<Recipe> RemoveRatingAsync(
    Guid recipeId,
    [Service] ApplicationDbContext context,
    [Service] IHttpContextAccessor httpContextAccessor,
    [Service] ICacheService cacheService,
    CancellationToken token)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var recipe = await context.Recipes.FindAsync(new object?[] { recipeId }, token);

        if (recipe is null)
        {
            throw new GraphQLException(new Error("Recipe not found.", "RECIPE_NOT_FOUND"));
        }
        var existingRating = await context.Ratings
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId && r.UserId == userId, token);

        if (existingRating != null)
        {
            context.Ratings.Remove(existingRating);
            await context.SaveChangesAsync(token);
        }

        var remainingRatings = context.Ratings.Where(r => r.RecipeId == recipeId);
        if (await remainingRatings.AnyAsync(token))
        {
            recipe.AverageRating = (float)await remainingRatings.AverageAsync(r => r.Score, token);
        }
        else
        {
            recipe.AverageRating = 0;
        }
        await context.SaveChangesAsync(token);

        string cacheKey = $"Recipe-{recipeId}";
        await cacheService.RemoveAsync(cacheKey, token);

        return recipe;
    }
}
