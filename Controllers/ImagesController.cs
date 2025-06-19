using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Receptoria.API.Data;
using Receptoria.API.Models;
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

    public ImagesController(ApplicationDbContext context, ICacheService cacheService, ILogger<ImagesController> logger)
    {
        _context = context;
        _cacheService = cacheService;
    }

    [HttpGet("recipe/{recipeId:guid}")]
    public async Task<IActionResult> GetRecipeImage(
        [FromRoute] Guid recipeId,
        [FromQuery] int? w, [FromQuery] int? h)
    {
        string format = CreateCacheKeyAndImageFormat<Recipe>(recipeId.ToString(), w, h, out var cacheKey);

        var cachedImage = await _cacheService.GetAsync<byte[]>(cacheKey);
        if (cachedImage != null)
        {
            Response.Headers.CacheControl = "public,max-age=86400";
            return File(cachedImage, $"image/{format}");
        }

        var originalImageBytes = await _context.Recipes.AsNoTracking()
            .Where(r => r.Id == recipeId)
            .Select(r => r.Image)
            .FirstOrDefaultAsync();

        if (originalImageBytes == null || originalImageBytes.Length == 0)
        {
            return NotFound();
        }

        var processedImageFile = await ProcessAndCacheImage(originalImageBytes, cacheKey);

        Response.Headers.CacheControl = "public,max-age=86400";
        return File(processedImageFile, $"image/{format}");
    }

    [HttpGet("step/{stepId:guid}")]
    public async Task<IActionResult> GetStepImage(
        [FromRoute] Guid stepId,
        [FromQuery] int? w, [FromQuery] int? h)
    {
        string format = CreateCacheKeyAndImageFormat<Step>(stepId.ToString(), w, h, out var cacheKey);

        var cachedImage = await _cacheService.GetAsync<byte[]>(cacheKey);
        if (cachedImage != null)
        {
            Response.Headers.CacheControl = "public,max-age=86400";
            return File(cachedImage, $"image/{format}");
        }

        var originalImageBytes = await _context.Steps.AsNoTracking()
            .Where(s => s.Id == stepId)
            .Select(s => s.Image)
            .FirstOrDefaultAsync();

        if (originalImageBytes == null || originalImageBytes.Length == 0) return NotFound();

        var processedImageFile = await ProcessAndCacheImage(originalImageBytes, cacheKey, w, h);

        Response.Headers.CacheControl = "public,max-age=86400";
        return File(processedImageFile, $"image/{format}");
    }


    [HttpGet("avatar/{userId}")]
    public async Task<IActionResult> GetUserAvatar(
        [FromRoute] string userId,
        [FromQuery] int? w, [FromQuery] int? h)
    {
        string format = CreateCacheKeyAndImageFormat<Step>(userId, w, h, out var cacheKey);

        var cachedImage = await _cacheService.GetAsync<byte[]>(cacheKey);
        if (cachedImage != null)
        {
            Response.Headers.CacheControl = "public,max-age=86400";
            return File(cachedImage, $"image/{format}");
        }

        var originalImageBytes = await _context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Avatar)
            .FirstOrDefaultAsync();

        if (originalImageBytes == null || originalImageBytes.Length == 0) return NotFound();

        var processedImageFile = await ProcessAndCacheImage(originalImageBytes, cacheKey, w, h);

        Response.Headers.CacheControl = "public,max-age=86400";
        return File(processedImageFile, $"image/{format}");
    }

    private async Task<byte[]> ProcessAndCacheImage(byte[] originalImageBytes, string cacheKey, int? width = null, int? height = null)
    {
        using var image = Image.Load(originalImageBytes);

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

        if (cacheKey.EndsWith("webp"))
        {
            await image.SaveAsync(stream, new WebpEncoder { Quality = 80 });
        }
        else
        {
            await image.SaveAsync(stream, new JpegEncoder { Quality = 85 });
        }

        var processedImageBytes = stream.ToArray();

        await _cacheService.SetAsync(cacheKey, processedImageBytes, TimeSpan.FromDays(7));

        return processedImageBytes;
    }
    private string CreateCacheKeyAndImageFormat<T>(string stepId, int? w, int? h, out string cacheKey)
    {
        var useWebp = Request.Headers.Accept.Contains("image/webp");
        var format = useWebp ? "webp" : "jpeg";
        cacheKey = $"ProcessedImage-{nameof(T)}-{stepId}-w{w ?? 0}-h{h ?? 0}-{format}";
        return format;
    }
}