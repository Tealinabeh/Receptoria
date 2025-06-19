using System.Security.Claims;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Receptoria.API.Data;
using Receptoria.API.Models;
using Receptoria.API.Services;

namespace Receptoria.API.GraphQL
{
    public class Query
    {
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Recipe> GetRecipes(ApplicationDbContext context)
        {
            return context.Recipes
               .AsNoTracking()
               .Include(r => r.Steps);
        }

        [UseProjection]
        public async Task<Recipe?> GetRecipeById(
            Guid id,
            ApplicationDbContext context,
            [Service] ICacheService cacheService, 
            CancellationToken token)
        {
            string cacheKey = $"Recipe-{id}";

            var cachedRecipe = await cacheService.GetAsync<Recipe>(cacheKey, token);
            if (cachedRecipe is not null)
            {
                return cachedRecipe;
            }

            var recipeFromDb = await context.Recipes
               .AsNoTracking()
               .Include(r => r.Author)
               .Include(r => r.Steps)
               .FirstOrDefaultAsync(r => r.Id == id, token);

            if (recipeFromDb is not null)
            {
                await cacheService.SetAsync(cacheKey, recipeFromDb, TimeSpan.FromHours(1), token);
            }

            return recipeFromDb;
        }

        [UseProjection]
        public async Task<Recipe?> GetDailyRecipe(
            ApplicationDbContext context,
            [Service] ICacheService cacheService,
            CancellationToken token)
        {
            const string DailyRecipeCacheKey = "DailyRecipe";

            var dailyRecipe = await cacheService.GetAsync<Recipe>(DailyRecipeCacheKey, token);
            if (dailyRecipe is not null)
            {
                return dailyRecipe;
            }

            var recipeFromDb = await context.Recipes
                .AsNoTracking()
                .Where(r => r.AverageRating >= 4)
                .OrderBy(r => Guid.NewGuid())
                .FirstOrDefaultAsync(token);

            if (recipeFromDb != null)
            {
                await cacheService.SetAsync(DailyRecipeCacheKey, recipeFromDb, TimeSpan.FromHours(24), token);
            }

            return recipeFromDb;
        }

        [Authorize]
        [UseOffsetPaging(IncludeTotalCount = true)]
        [UseFiltering]
        [UseSorting]
        public async Task<IEnumerable<Recipe>> GetMyFavorites(
            [Service] ApplicationDbContext context,
            [Service] UserManager<ReceptoriaUser> userManager,
            [Service] IHttpContextAccessor httpContextAccessor,
            CancellationToken token)
        {
            var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
            {
                return Enumerable.Empty<Recipe>();
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user?.FavoriteRecipes is null || user.FavoriteRecipes.Length == 0)
            {
                return Enumerable.Empty<Recipe>();
            }

            var favoriteRecipeIds = user.FavoriteRecipes.Select(Guid.Parse).ToList();

            return await context.Recipes
                .Where(r => favoriteRecipeIds.Contains(r.Id))
                .Include(r => r.Steps)
                .ToListAsync(token);
        }

        [Authorize]
        public async Task<ReceptoriaUser?> GetMe([Service] UserManager<ReceptoriaUser> userManager, [Service] IHttpContextAccessor httpContextAccessor)
        {
            var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return null;

            return await userManager.FindByIdAsync(userId);
        }

        public async Task<ReceptoriaUser?> GetUserById(
            string id,
            [Service] UserManager<ReceptoriaUser> userManager)
        {
            return await userManager.FindByIdAsync(id);
        }

        [Authorize]
        public async Task<Rating?> GetMyRatingForRecipe(
            Guid recipeId,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor httpContextAccessor,
            CancellationToken token)
        {
            var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
            {
                return null;
            }

            return await context.Ratings
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RecipeId == recipeId && r.UserId == userId, token);
        }
    }
}