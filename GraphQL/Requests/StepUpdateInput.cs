namespace Receptoria.API.GraphQL.Requests;
public record StepUpdateInput(
    int StepNumber,
    string Description,
    IFile? Image,
    bool? RemoveImage = false
);