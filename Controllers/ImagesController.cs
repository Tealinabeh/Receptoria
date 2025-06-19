using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Receptoria.API.Data;
using SixLabors.ImageSharp.Formats.Webp;

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
    public async Task<IActionResult> GetRecipeImage(
        [FromRoute] Guid recipeId,
        [FromQuery] int? w, 
        [FromQuery] int? h  
    )
    {
        var recipe = await _context.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == recipeId);
        if (recipe?.Image == null || recipe.Image.Length == 0) return NotFound();

        return await ProcessAndServeImage(recipe.Image, w, h);
    }

    [HttpGet("step/{stepId:guid}")]
    public async Task<IActionResult> GetStepImage(
        [FromRoute] Guid stepId,
        [FromQuery] int? w,
        [FromQuery] int? h
    )
    {
        var step = await _context.Steps.AsNoTracking().FirstOrDefaultAsync(s => s.Id == stepId);
        if (step?.Image == null || step.Image.Length == 0) return NotFound();

        return await ProcessAndServeImage(step.Image, w, h);
    }

    [HttpGet("avatar/{userId}")]
    public async Task<IActionResult> GetUserAvatar(
        [FromRoute] string userId,
        [FromQuery] int? w,
        [FromQuery] int? h
    )
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Avatar == null || user.Avatar.Length == 0) return NotFound();

        return await ProcessAndServeImage(user.Avatar, w, h);
    }

    private async Task<IActionResult> ProcessAndServeImage(byte[] imageBytes, int? width, int? height)
    {
        bool useWebp = Request.Headers["Accept"].ToString().Contains("image/webp");

        using var image = Image.Load(imageBytes);

        if (width.HasValue || height.HasValue)
        {
            var resizeOptions = new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(width ?? 0, height ?? 0)
            };

            if (!width.HasValue) resizeOptions.Size = new Size(image.Width, height!.Value);
            if (!height.HasValue) resizeOptions.Size = new Size(width!.Value, image.Height);

            image.Mutate(x => x.Resize(resizeOptions));
        }
        var stream = new MemoryStream();
        if (useWebp)
        {
            await image.SaveAsync(stream, new WebpEncoder { Quality = 80 });
            stream.Position = 0;
            Response.Headers.CacheControl = "public,max-age=86400";
            return File(stream, "image/webp");
        }
        else
        {
            await image.SaveAsJpegAsync(stream);
            stream.Position = 0;
            Response.Headers.CacheControl = "public,max-age=86400";
            return File(stream, "image/jpeg");
        }
    }
}
