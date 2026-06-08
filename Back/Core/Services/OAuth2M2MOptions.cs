namespace DocumentPortalIam.Back.Core.Services;

public sealed class OAuth2M2MOptions
{
    public string ClientId { get; set; } = "storage-client";
    public string ClientSecret { get; set; } = "M2M@123";
    public string Scope { get; set; } = "exports.m2m";
}
