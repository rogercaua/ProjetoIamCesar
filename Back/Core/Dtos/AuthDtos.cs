using System.Text.Json.Serialization;

namespace DocumentPortalIam.Back.Core.Dtos;

public sealed class LoginRequestDto
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class AuthenticatedUserDto
{
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
}

public sealed class UserDto
{
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}

public sealed class ClientCredentialsRequestDto
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = "";

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = "";
}

public sealed class TokenResponseDto
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    public string Scope { get; set; } = "";
}
