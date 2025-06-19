using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Receptoria.API.Models;

namespace Receptoria.API.Data
{
    public static class DbInitializer
    {
        private const int RECIPE_AMOUNT = 234;
        private const int USER_AMOUNT = 67;
        private const int MIN_CATEGORIES_AMOUNT = 1;
        private const int MAX_CATEGORIES_AMOUNT = 8;
        private const int MIN_STEPS_AMOUNT = 3;
        private const int MAX_STEPS_AMOUNT = 14;
        private const int MIN_TIME_TO_COOK = 5;     
        private const int MAX_TIME_TO_COOK = 121;    
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ReceptoriaUser>>();

            if (await context.Recipes.AnyAsync())
            {
                return;
            }

            var allCategories = new List<string>
            {
                "сніданок", "обід", "вечеря", "салат", "десерт", "паштет", "намазка", "випічка", "тісто", "гриль",
                "веганське", "пісне", "безглютенове", "безцукрове", "низькокалорійне", "здорове", "фітнес", "дитяче",
                "святкове", "на природі", "літнє", "осіннє", "гаряче", "холодне"
            };

            var random = new Random();

            for (int i = 1; i <= USER_AMOUNT; i++)
            {
                string avatarPath = $"assets/Avatars/Placeholder_{i}.jpeg";
                if (!File.Exists(avatarPath))
                {
                    Console.WriteLine($"Error: Avatar file not found at path: {avatarPath}");
                    continue;
                }

                var user = new ReceptoriaUser
                {
                    UserName = $"User{i}",
                    Email = $"user{i}@example.com",
                    EmailConfirmed = true,
                    Bio = $"Це біографія користувача №{i}. Я люблю готувати та ділитися рецептами!",
                    Avatar = await File.ReadAllBytesAsync(avatarPath),
                    RegistrationDate = DateTime.UtcNow.AddDays(-i).AddMonths(-i),
                };

                var result = await userManager.CreateAsync(user, "Password123!");
                if (!result.Succeeded)
                {
                    Console.WriteLine($"Error creating user {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            var createdUsers = await context.Users.AsNoTracking().ToListAsync();
            var recipes = new List<Recipe>();
            var allRatings = new List<Rating>(); 
            var categoriesToEnsure = new HashSet<string>(allCategories);

            for (int i = 0; i < RECIPE_AMOUNT; i++)
            {
                var author = createdUsers[random.Next(createdUsers.Count)];

                var recipeCategories = new HashSet<string>();
                if (categoriesToEnsure.Any())
                {
                    var categoryToAdd = categoriesToEnsure.First();
                    recipeCategories.Add(categoryToAdd);
                    categoriesToEnsure.Remove(categoryToAdd);
                }
                else
                {
                    recipeCategories.Add(allCategories[random.Next(allCategories.Count)]);
                }
                for (int j = 0; j < random.Next(MIN_CATEGORIES_AMOUNT, MAX_CATEGORIES_AMOUNT); j++)
                {
                    recipeCategories.Add(allCategories[random.Next(allCategories.Count)]);
                }

                var ingredients = new[] { "Картопля", "Куряче філе", $"Спеції для рецепту {i + 1}", "Сіль та перець за смаком" };

                string recipeImagePath = $"assets/RecipeImages/Placeholder_{(i % 15) + 1}.jpeg";
                if (!File.Exists(recipeImagePath))
                {
                    Console.WriteLine($"Error: Recipe image file not found: {recipeImagePath}");
                    continue;
                }

                var recipe = new Recipe
                {
                    Id = Guid.NewGuid(),
                    Title = $"Рецепт страви №{i + 1}",
                    Description = $"Детальний опис для рецепту №{i + 1}. Дуже простий у приготуванні та сподобається всій родині.",
                    Difficulty = random.Next(1, 4), 
                    TimeToCook = random.Next(MIN_TIME_TO_COOK, MAX_TIME_TO_COOK),
                    AuthorId = author.Id,
                    Categories = recipeCategories.ToArray(),
                    Ingredients = ingredients,
                    IngredientCount = ingredients.Length,
                    Image = await File.ReadAllBytesAsync(recipeImagePath),
                    Created = DateTime.UtcNow.AddDays(-random.Next(1, 100))
                };

                var steps = new List<Step>();
                for (int j = 1; j <= random.Next(MIN_STEPS_AMOUNT, MAX_STEPS_AMOUNT); j++)
                {
                    steps.Add(new Step
                    {
                        Id = Guid.NewGuid(),
                        RecipeId = recipe.Id,
                        StepNumber = j,
                        Description = $"Це опис для кроку №{j}. Виконайте цю дію, а потім переходьте до наступного.",
                        Image = null
                    });
                }
                recipe.Steps = steps;

                var recipeRatings = new List<Rating>();
                var usersWhoRated = new HashSet<string>();
                var totalScore = 0;
                var ratingsCount = random.Next(0, createdUsers.Count);

                for (int j = 0; j < ratingsCount; j++)
                {
                    var ratingUser = createdUsers[random.Next(createdUsers.Count)];
                    if (ratingUser.Id != author.Id && !usersWhoRated.Contains(ratingUser.Id))
                    {
                        var score = random.Next(1, 6); 
                        totalScore += score;
                        usersWhoRated.Add(ratingUser.Id);

                        recipeRatings.Add(new Rating
                        {
                            Id = Guid.NewGuid(),
                            RecipeId = recipe.Id,
                            UserId = ratingUser.Id,
                            Score = score
                        });
                    }
                }

                if (recipeRatings.Any())
                {
                    recipe.AverageRating = (float)Math.Round((float)totalScore / recipeRatings.Count, 1);
                    allRatings.AddRange(recipeRatings); 
                }
                else
                {
                    recipe.AverageRating = 0;
                }

                recipes.Add(recipe);
            }

            await context.Recipes.AddRangeAsync(recipes);
            await context.Ratings.AddRangeAsync(allRatings);

            foreach (var user in createdUsers)
            {
                var userToUpdate = await userManager.FindByIdAsync(user.Id);
                if (userToUpdate == null) continue;

                var favoriteRecipes = recipes
                    .Where(r => r.AuthorId != user.Id)
                    .OrderBy(r => random.Next())
                    .Take(random.Next(5, 11))
                    .Select(r => r.Id.ToString())
                    .ToArray();

                userToUpdate.FavoriteRecipes = favoriteRecipes;
                await userManager.UpdateAsync(userToUpdate);
            }

            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while saving data: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }
    }
}