using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Receptoria.API.Data;
using Receptoria.API.Services;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;

namespace Receptoria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICacheService _cacheService; 

    public ImagesController(ApplicationDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    [HttpGet("recipe/{recipeId:guid}")]
    public async Task<IActionResult> GetRecipeImage(
        [FromRoute] Guid recipeId,
        [FromQuery] int? w, [FromQuery] int? h)
    {
        var imageKey = $"OriginalImage-Recipe-{recipeId}";

        var imageBytes = await GetOriginalImageBytes(imageKey,
            () => _context.Recipes.AsNoTracking().Where(r => r.Id == recipeId).Select(r => r.Image).FirstOrDefaultAsync());

        if (imageBytes == null || imageBytes.Length == 0) return NotFound();

        return await ProcessAndServeImage(imageBytes, $"Recipe-{recipeId}", w, h);
    }

    [HttpGet("step/{stepId:guid}")]
    public async Task<IActionResult> GetStepImage(
        [FromRoute] Guid stepId,
        [FromQuery] int? w, [FromQuery] int? h)
    {
        var imageKey = $"OriginalImage-Step-{stepId}";
        var imageBytes = await GetOriginalImageBytes(imageKey,
            () => _context.Steps.AsNoTracking().Where(s => s.Id == stepId).Select(s => s.Image).FirstOrDefaultAsync());

        if (imageBytes == null || imageBytes.Length == 0) return NotFound();

        return await ProcessAndServeImage(imageBytes, $"Step-{stepId}", w, h);
    }

    [HttpGet("avatar/{userId}")]
    public async Task<IActionResult> GetUserAvatar(
        [FromRoute] string userId,
        [FromQuery] int? w, [FromQuery] int? h)
    {
        var imageKey = $"OriginalImage-Avatar-{userId}";
        var imageBytes = await GetOriginalImageBytes(imageKey,
            () => _context.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Avatar).FirstOrDefaultAsync());

        if (imageBytes == null || imageBytes.Length == 0) return NotFound();

        return await ProcessAndServeImage(imageBytes, $"Avatar-{userId}", w, h);
    }

    private async Task<byte[]?> GetOriginalImageBytes(string key, Func<Task<byte[]?>> dbFallback)
    {
        var cachedImage = await _cacheService.GetAsync<byte[]>(key);
        if (cachedImage != null) return cachedImage;

        var imageFromDb = await dbFallback();
        if (imageFromDb != null)
        {
            await _cacheService.SetAsync(key, imageFromDb, TimeSpan.FromDays(7));
        }
        return imageFromDb;
    }

    private async Task<IActionResult> ProcessAndServeImage(byte[] imageBytes, string imageIdentifier, int? width, int? height)
    {
        bool useWebp = Request.Headers.Accept.ToString().Contains("image/webp");
        string format = useWebp ? "webp" : "jpeg";

        string cacheKey = $"ProcessedImage-{imageIdentifier}-w{width ?? 0}-h{height ?? 0}-{format}";

        var cachedProcessedImage = await _cacheService.GetAsync<byte[]>(cacheKey);
        if (cachedProcessedImage != null)
        {
            Response.Headers.CacheControl = "public,max-age=86400"; // 1 день
            return File(cachedProcessedImage, $"image/{format}");
        }
        using var image = Image.Load(imageBytes);

        if (width.HasValue || height.HasValue)
        {
            var resizeOptions = new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(width ?? image.Width, height ?? image.Height)
            };
            image.Mutate(x => x.Resize(resizeOptions));
        }

        await using var stream = new MemoryStream();

        if (useWebp)
        {
            await image.SaveAsync(stream, new WebpEncoder { Quality = 80 });
        }
        else
        {
            await image.SaveAsync(stream, new JpegEncoder { Quality = 85 });
        }

        var processedImageBytes = stream.ToArray();

        await _cacheService.SetAsync(cacheKey, processedImageBytes, TimeSpan.FromDays(7));

        Response.Headers.CacheControl = "public,max-age=86400";
        return File(processedImageBytes, $"image/{format}");
    }
}