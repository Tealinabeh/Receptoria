using Receptoria.API.Models;

namespace Receptoria.API.GraphQL.Payloads;
public record AuthPayload(string Token, ReceptoriaUser User);