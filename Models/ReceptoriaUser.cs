using Microsoft.AspNetCore.Identity;

namespace Receptoria.API.Models;

public sealed class ReceptoriaUser : IdentityUser
{
    public string[] FavoriteRecipes { get; set; } = Array.Empty<string>();
    [GraphQLIgnore]
    public byte[]? Avatar { get; set; }
    public string? Bio { get; set; }
    public DateTime RegistrationDate { get; set; }
}