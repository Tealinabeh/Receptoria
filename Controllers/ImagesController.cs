using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Receptoria.API.Data;

namespace Receptoria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ImagesController(ApplicationDbContext context)
    {
        _context = context;
    }
    [HttpGet("recipe/{recipeId:guid}")]
    public async Task<IActionResult> GetRecipeImage([FromRoute]Guid recipeId)
    {
        var recipe = await _context.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == recipeId);
        if (recipe?.MainImage == null || recipe.MainImage.Length == 0) return NotFound();
        return File(recipe.MainImage, "image/jpeg");
    }

    [HttpGet("step/{stepId:guid}")]
    public async Task<IActionResult> GetStepImage([FromRoute]Guid stepId)
    {
        var step = await _context.Steps.AsNoTracking().FirstOrDefaultAsync(s => s.Id == stepId);
        if (step?.Image == null || step.Image.Length == 0) return NotFound();
        return File(step.Image, "image/jpeg");
    }

    [HttpGet("avatar/{userId:guid}")]
    public async Task<IActionResult> GetUserAvatar([FromRoute]string userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.Avatar == null || user.Avatar.Length == 0)
        {
            return NotFound();
        }
        return File(user.Avatar, "image/jpeg");
    }
}