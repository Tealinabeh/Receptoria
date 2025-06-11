namespace Receptoria.API.GraphQL.Requests;
public record StepInput(
    string Description,
    IFile? Image
);