using DocumentPortalIam.Back.Core.Data;
using DocumentPortalIam.Back.Core.Services;
using DocumentPortalIam.Back.Core.Swagger;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerUI;

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
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Portal IAM Docs API",
        Version = "v1",
        Description = """
        API do projeto pratico de IAM.

        Como testar:
        1. Execute POST /api/auth/login com admin/Admin@123, gestor/Gestor@123, aluno/Aluno@123 ou auditor/Auditor@123.
        2. Depois do login, o Swagger usa o cookie da sessao para testar rotas protegidas.
        3. Para Google Drive, conecte a conta pelo painel em /dashboard.
        4. Para M2M, execute POST /api/oauth/token, copie o access_token e use no header Authorization da rota /api/m2m/export/{id}.

        Regras principais:
        - Administrador: controle total.
        - Gestor: documentos e Google Drive, sem auditoria e sem troca de papeis.
        - Usuario/aluno: apenas documentos Publico.
        - Auditor: apenas auditoria.
        """
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "M2M token",
        Description = """
        Token para o fluxo OAuth2 M2M.

        Como usar:
        1. Execute POST /api/oauth/token.
        2. Copie o access_token retornado.
        3. Clique em Authorize.
        4. Cole apenas o token, sem escrever Bearer.

        O Swagger enviara o header Authorization: Bearer {token}.
        """
    });
    options.EnableAnnotations();
    options.OperationFilter<SwaggerUsageOperationFilter>();
    options.TagActionsBy(apiDescription =>
    {
        var controller = apiDescription.ActionDescriptor.RouteValues["controller"];
        return new[]
        {
            controller switch
            {
                "Auth" => "01 - Login LDAP e sessao",
                "Documents" => "02 - Documentos",
                "Google" => "03 - Google OIDC e Drive",
                "Users" => "04 - Governanca LDAP",
                "Rbac" => "05 - Matriz RBAC",
                "Audit" => "06 - Auditoria",
                "OAuth" => "07 - OAuth2 M2M Token",
                "M2M" => "08 - Exportacao M2M",
                _ => controller ?? "Outros"
            }
        };
    });
    options.OrderActionsBy(apiDescription =>
        $"{apiDescription.ActionDescriptor.RouteValues["controller"]}_{apiDescription.HttpMethod}_{apiDescription.RelativePath}");
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
    options.DisplayRequestDuration();
    options.DocExpansion(DocExpansion.List);
    options.EnableDeepLinking();
    options.EnableFilter();
    options.DefaultModelsExpandDepth(1);
    options.DefaultModelExpandDepth(2);
    options.ConfigObject.AdditionalItems["withCredentials"] = true;
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
