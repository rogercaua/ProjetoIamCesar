using DocumentPortalIam.Back.Core.Dtos;

namespace DocumentPortalIam.Back.Core.Services;

public interface IM2MTokenService
{
    TokenResponseDto? IssueToken(string clientId, string clientSecret);
    bool Validate(string accessToken, string requiredScope, out string clientId);
}
