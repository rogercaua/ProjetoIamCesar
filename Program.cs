using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine("Front", "wwwroot")
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/Front/Pages";
});
builder.Services.AddControllers();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "Storage", "data-protection-keys")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Denied";
        options.Cookie.Name = "DocumentPortalIam.Auth";
    });

builder.Services.Configure<LdapDemoOptions>(builder.Configuration.GetSection("LdapDemo"));
builder.Services.Configure<OAuth2M2MOptions>(builder.Configuration.GetSection("OAuth2M2M"));
builder.Services.AddSingleton<IDirectoryService, DemoLdapDirectoryService>();
builder.Services.AddSingleton<IRbacService, RbacService>();
builder.Services.AddSingleton<IAuditService, AuditService>();
builder.Services.AddSingleton<IDocumentRepository, DocumentRepository>();
builder.Services.AddSingleton<IM2MTokenService, M2MTokenService>();
builder.Services.AddSingleton<IM2MStorageExportService, M2MStorageExportService>();
builder.Services.AddSingleton<IOidcExportService, OidcExportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
