using DocumentPortalIam.Back.Core.Data;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine("Front", "wwwroot")
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
});
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "Storage", "data-protection-keys")));

var storageRoot = Path.Combine(builder.Environment.ContentRootPath, "Storage");
Directory.CreateDirectory(storageRoot);

var sqliteConnection = builder.Configuration.GetConnectionString("Default")
    ?? $"Data Source={Path.Combine(storageRoot, "iam-documents.db")}";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(sqliteConnection);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/";
        options.AccessDeniedPath = "/";
        options.Cookie.Name = "DocumentPortalIam.Auth";
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    })
    .AddCookie("GoogleExternal", options =>
    {
        options.Cookie.Name = "DocumentPortalIam.Google";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    })
    .AddOpenIdConnect("Google", options =>
    {
        options.SignInScheme = "GoogleExternal";
        options.Authority = "https://accounts.google.com";
        options.ClientId = builder.Configuration["Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
        options.CallbackPath = "/signin-google";
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("https://www.googleapis.com/auth/drive.file");
    });

builder.Services.Configure<LdapOptions>(builder.Configuration.GetSection("Ldap"));
builder.Services.Configure<OAuth2M2MOptions>(builder.Configuration.GetSection("OAuth2M2M"));
builder.Services.Configure<GoogleDriveOptions>(builder.Configuration.GetSection("GoogleDrive"));
builder.Services.AddScoped<IDirectoryService, LdapDirectoryService>();
builder.Services.AddSingleton<IRbacService, RbacService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddSingleton<IM2MTokenService, M2MTokenService>();
builder.Services.AddScoped<IM2MStorageExportService, M2MStorageExportService>();
builder.Services.AddScoped<IGoogleDriveExportService, GoogleDriveExportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "Portal IAM Docs API";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Portal IAM Docs API v1");
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Context.Response.Headers.Pragma = "no-cache";
        context.Context.Response.Headers.Expires = "0";
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    database.Database.EnsureCreated();
}

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
