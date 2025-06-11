namespace Receptoria.API.GraphQL.Requests;

public record LoginUserInput(
    string Email,
    string Password
);