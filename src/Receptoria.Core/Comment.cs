namespace Receptoria.Core;

public record Comment
{
    public string Id { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = default!;
}
