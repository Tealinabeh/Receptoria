using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Receptoria.API.Extensions;
using Receptoria.API.GraphQL.Payloads;
using Receptoria.API.GraphQL.Requests;
using Receptoria.API.Models;
using Receptoria.API.Services;

namespace Receptoria.API.GraphQL;

public class Mutation
{
    [AllowAnonymous]
    public async Task<AuthPayload> RegisterUserAsync(
        RegisterUserInput input,
        [Service] UserManager<ReceptoriaUser> userManager,
        [Service] TokenService tokenService,
        CancellationToken cancellationToken)
    {
        var user = new ReceptoriaUser
        {
            UserName = input.Username,
            Email = input.Email,
            Avatar = await input.Image.ToByteArrayAsync(cancellationToken),
            RegistrationDate = DateTime.UtcNow,
        };

        var result = await userManager.CreateAsync(user, input.Password);

        if (!result.Succeeded)
        {
            throw new GraphQLException(new Error(
                string.Join("\n", result.Errors.Select(e => e.Description)),
                "REGISTRATION_FAILED"));
        }

        var token = tokenService.CreateToken(user);
        return new AuthPayload(token, user);
    }

    [AllowAnonymous]
    public async Task<AuthPayload> LoginUserAsync(
            LoginUserInput input,
            [Service] UserManager<ReceptoriaUser> userManager,
            [Service] TokenService tokenService)
    {
        var user = await userManager.FindByEmailAsync(input.Email);

        if (user == null || !await userManager.CheckPasswordAsync(user, input.Password))
        {
            throw new GraphQLException(new Error("Invalid credentials.", "LOGIN_FAILED"));
        }

        var token = tokenService.CreateToken(user);
        return new AuthPayload(token, user);
    }
}