namespace Receptoria.API.GraphQL.Requests;
public record UpdateProfileInput(
    string? UserName,
    string? Bio,
    IFile? Avatar,
    string? NewEmail,
    string? CurrentPassword,
    string? NewPassword
);