using System.Security.Cryptography;
using DocumentPortalIam.Back.Core.Dtos;
using DocumentPortalIam.Back.Core.Models;
using Microsoft.Extensions.Options;

namespace DocumentPortalIam.Back.Core.Services;

public sealed class M2MTokenService : IM2MTokenService
{
    private readonly OAuth2M2MOptions _options;
    private readonly Dictionary<string, IssuedToken> _tokens = new();

    public M2MTokenService(IOptions<OAuth2M2MOptions> options)
    {
        _options = options.Value;
    }

    public TokenResponseDto? IssueToken(string clientId, string clientSecret)
    {
        if (clientId != _options.ClientId || clientSecret != _options.ClientSecret)
        {
            return null;
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var issued = new IssuedToken(clientId, Permissions.ExportM2M, DateTimeOffset.UtcNow.AddMinutes(30));
        lock (_tokens)
        {
            _tokens[token] = issued;
        }

        return new TokenResponseDto
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = 1800,
            Scope = Permissions.ExportM2M
        };
    }

    public bool Validate(string accessToken, string requiredScope, out string clientId)
    {
        clientId = "";
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        lock (_tokens)
        {
            if (!_tokens.TryGetValue(accessToken, out var token))
            {
                return false;
            }

            if (token.ExpiresAt < DateTimeOffset.UtcNow || token.Scope != requiredScope)
            {
                return false;
            }

            clientId = token.ClientId;
            return true;
        }
    }

    private sealed record IssuedToken(string ClientId, string Scope, DateTimeOffset ExpiresAt);
}
