using System.Security.Claims;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Receptoria.API.Data;
using Receptoria.API.GraphQL.Requests;
using Receptoria.API.Models;

namespace Receptoria.API.GraphQL;

[Authorize]
[ExtendObjectType(typeof(Mutation))]
public class ProfileMutation
{
    public async Task<ReceptoriaUser> AddFavoriteRecipeAsync(
        Guid recipeId,
        [Service] ApplicationDbContext context,
        [Service] UserManager<ReceptoriaUser> userManager,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken token)
    {
        if (!await context.Recipes.AnyAsync(r => r.Id == recipeId, token))
        {
            throw new GraphQLException(new Error("Recipe not found.", "RECIPE_NOT_FOUND"));
        }

        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await userManager.FindByIdAsync(userId!);

        if (user is null)
        {
            throw new GraphQLException(new Error("User not found.", "USER_NOT_FOUND"));
        }

        var favoriteId = recipeId.ToString();
        if (!user.FavoriteRecipes.Contains(favoriteId))
        {
            user.FavoriteRecipes = user.FavoriteRecipes.Append(favoriteId).ToArray();

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new GraphQLException(new Error("Failed to update user favorites.", "UPDATE_FAILED"));
            }
        }

        return user;
    }

    public async Task<ReceptoriaUser> RemoveFavoriteRecipeAsync(
        Guid recipeId,
        [Service] UserManager<ReceptoriaUser> userManager,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await userManager.FindByIdAsync(userId!);

        if (user is null)
        {
            throw new GraphQLException(new Error("User not found.", "USER_NOT_FOUND"));
        }

        var favoriteId = recipeId.ToString();
        if (user.FavoriteRecipes.Contains(favoriteId))
        {
            user.FavoriteRecipes = user.FavoriteRecipes.Where(id => id != favoriteId).ToArray();

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new GraphQLException(new Error("Failed to update user favorites.", "UPDATE_FAILED"));
            }
        }

        return user;
    }

    public async Task<ReceptoriaUser> UpdateProfileAsync(
        UpdateProfileInput input,
        [Service] UserManager<ReceptoriaUser> userManager,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            throw new GraphQLException("User not found.");
        }
        if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            user.UserName = input.UserName;
        }
        if (input.Bio is not null)
        {
            user.Bio = input.Bio;
        }
        if (input.Avatar is not null)
        {
            await using var memoryStream = new MemoryStream();
            await input.Avatar.CopyToAsync(memoryStream);
            user.Avatar = memoryStream.ToArray();
        }
        if (!string.IsNullOrWhiteSpace(input.NewEmail) && user.Email != input.NewEmail)
        {
            await userManager.SetEmailAsync(user, input.NewEmail);
            await userManager.UpdateNormalizedEmailAsync(user);
        }
        if (!string.IsNullOrWhiteSpace(input.NewPassword) && !string.IsNullOrWhiteSpace(input.CurrentPassword))
        {
            var passwordResult = await userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
            if (!passwordResult.Succeeded)
            {
                throw new GraphQLException(new Error(string.Join(", ", passwordResult.Errors.Select(e => e.Description))));
            }
        }

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new GraphQLException(new Error(string.Join(", ", updateResult.Errors.Select(e => e.Description))));
        }

        return user;
    }
}
