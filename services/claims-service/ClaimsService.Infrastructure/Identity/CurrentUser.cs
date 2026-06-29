using System.IdentityModel.Tokens.Jwt;
using ClaimsService.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ClaimsService.Infrastructure.Identity;
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _contextAccessor;

    public CurrentUser(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    private JwtSecurityToken? GetToken()
    {
        var authHeader =
            _contextAccessor.HttpContext?
                .Request.Headers.Authorization
                .ToString();

        if (string.IsNullOrWhiteSpace(authHeader))
            return null;

        if (!authHeader.StartsWith("Bearer "))
            return null;

        var token = authHeader["Bearer ".Length..];

        return new JwtSecurityTokenHandler()
            .ReadJwtToken(token);
    }

    public string? UserId =>
        GetToken()?.Claims
            .FirstOrDefault(c => c.Type == "oid")
            ?.Value;

    public string? Email =>
        GetToken()?.Claims
            .FirstOrDefault(c => c.Type == "preferred_username")
            ?.Value;

    public string? Name =>
        GetToken()?.Claims
            .FirstOrDefault(c => c.Type == "name")
            ?.Value;
}