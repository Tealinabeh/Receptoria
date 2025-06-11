namespace Receptoria.API.GraphQL.Requests;

public record RegisterUserInput(
    string Username,
    string Email,
    string Password,
    IFile? Image
);