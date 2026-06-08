using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentPortalIam.Back.Controllers;

[ApiController]
[AllowAnonymous]
public sealed class SwaggerController : ControllerBase
{
    [HttpGet("/swagger")]
    public ContentResult Index()
    {
        const string html = """
        <!doctype html>
        <html lang="pt-br">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Swagger - Portal IAM Docs</title>
            <style>
                body { font-family: Segoe UI, Arial, sans-serif; margin: 32px; color: #152033; background: #f7f8fb; }
                h1 { margin-bottom: 6px; }
                p { color: #526173; }
                table { width: 100%; border-collapse: collapse; background: white; border: 1px solid #dce3ea; }
                th, td { padding: 12px; border-bottom: 1px solid #e4e9ef; text-align: left; vertical-align: top; }
                th { background: #eef3f7; }
                code { color: #0d6b6f; font-weight: 700; }
                .method { font-weight: 800; color: #0f3d5e; }
                a { color: #0d6b6f; }
            </style>
        </head>
        <body>
            <h1>Swagger - Portal IAM Docs</h1>
            <p>Documentacao manual OpenAPI. JSON tecnico: <a href="/swagger/v1/swagger.json">/swagger/v1/swagger.json</a>.</p>
            <table>
                <thead><tr><th>Metodo</th><th>Rota</th><th>Acesso</th><th>O que faz</th></tr></thead>
                <tbody>
                    <tr><td class="method">POST</td><td><code>/api/auth/login</code></td><td>Publico</td><td>Realiza login LDAP demo e cria cookie de sessao.</td></tr>
                    <tr><td class="method">GET</td><td><code>/api/auth/me</code></td><td>Usuario logado</td><td>Retorna usuario, papeis e permissoes da sessao.</td></tr>
                    <tr><td class="method">POST</td><td><code>/api/auth/logout</code></td><td>Usuario logado</td><td>Encerra a sessao autenticada.</td></tr>
                    <tr><td class="method">GET</td><td><code>/api/rbac/roles</code></td><td>Publico</td><td>Lista a matriz RBAC do sistema.</td></tr>
                    <tr><td class="method">GET</td><td><code>/api/documents</code></td><td>Logado; RBAC filtra retorno</td><td>Lista documentos visiveis pelo papel do usuario.</td></tr>
                    <tr><td class="method">POST</td><td><code>/api/documents</code></td><td>Admin, Gestor ou Usuario</td><td>Envia novo documento.</td></tr>
                    <tr><td class="method">GET</td><td><code>/api/documents/{id}/download</code></td><td>Admin/Gestor todos; Usuario apenas proprio</td><td>Baixa documento autorizado.</td></tr>
                    <tr><td class="method">DELETE</td><td><code>/api/documents/{id}</code></td><td>Apenas Administrador</td><td>Remove documento.</td></tr>
                    <tr><td class="method">POST</td><td><code>/api/documents/{id}/export/oidc</code></td><td>Admin, Gestor ou Usuario com acesso ao documento</td><td>Exporta documento pelo fluxo OIDC demo.</td></tr>
                    <tr><td class="method">GET</td><td><code>/api/users</code></td><td>Apenas Administrador</td><td>Lista usuarios do diretorio LDAP demo.</td></tr>
                    <tr><td class="method">PUT</td><td><code>/api/users/{userName}/role</code></td><td>Apenas Administrador</td><td>Altera o papel de um usuario.</td></tr>
                    <tr><td class="method">GET</td><td><code>/api/audit</code></td><td>Admin, Gestor ou Auditor</td><td>Mostra eventos de auditoria.</td></tr>
                    <tr><td class="method">POST</td><td><code>/api/oauth/token</code></td><td>Cliente M2M</td><td>Emite token OAuth2 client credentials.</td></tr>
                    <tr><td class="method">POST</td><td><code>/api/m2m/export/{id}</code></td><td>Bearer token com escopo exports.m2m</td><td>Exporta documento para storage externo M2M.</td></tr>
                </tbody>
            </table>
        </body>
        </html>
        """;

        return Content(html, "text/html");
    }

    [HttpGet("/swagger/v1/swagger.json")]
    public IActionResult OpenApi()
    {
        return Ok(new Dictionary<string, object?>
        {
            ["openapi"] = "3.0.3",
            ["info"] = new
            {
                title = "Portal IAM Docs API",
                version = "v1",
                description = "API do projeto IAM. As descricoes indicam quem pode acessar cada rota e qual requisito ela demonstra."
            },
            ["paths"] = new Dictionary<string, object?>
            {
                ["/api/auth/login"] = new { post = Operation("Auth", "Login LDAP", "Publico. Realiza login no diretorio LDAP demo, cria cookie de sessao e retorna usuario, papeis e permissoes.", "LoginRequestDto", "AuthenticatedUserDto", security: false) },
                ["/api/auth/me"] = new { get = Operation("Auth", "Sessao atual", "Usuario logado. Retorna os dados do usuario autenticado, seus papeis e permissoes RBAC.", responseSchema: "AuthenticatedUserDto") },
                ["/api/auth/logout"] = new { post = Operation("Auth", "Logout", "Usuario logado. Encerra o cookie de sessao e registra auditoria.", responseSchema: "MessageResponseDto") },
                ["/api/rbac/roles"] = new { get = Operation("RBAC", "Matriz RBAC", "Publico. Lista papeis, descricoes e permissoes para demonstrar a modelagem RBAC.", responseSchema: "RoleDto[]", security: false) },
                ["/api/documents"] = new
                {
                    get = Operation("Documents", "Listar documentos", "Usuario logado. Admin, Gestor e Auditor enxergam todos. Usuario comum enxerga apenas documentos proprios.", responseSchema: "DocumentDto[]"),
                    post = Operation("Documents", "Upload de documento", "Admin, Gestor ou Usuario. Recebe multipart/form-data com arquivo e classificacao, salva o documento e registra auditoria.", "UploadDocumentRequestDto", "DocumentDto")
                },
                ["/api/documents/{id}/download"] = new { get = Operation("Documents", "Baixar documento", "Admin e Gestor podem baixar todos. Usuario comum baixa apenas os proprios. Auditor nao baixa.", responseSchema: "arquivo binario", parameters: true) },
                ["/api/documents/{id}"] = new { delete = Operation("Documents", "Excluir documento", "Apenas Administrador. Remove o documento do storage local e registra auditoria.", responseSchema: "MessageResponseDto", parameters: true) },
                ["/api/documents/{id}/export/oidc"] = new { post = Operation("Exports", "Exportar via OIDC", "Admin, Gestor ou Usuario com acesso ao documento. Simula consentimento OIDC e exporta para Google Drive demo.", "OidcExportRequestDto", "ExportResultDto", parameters: true) },
                ["/api/users"] = new { get = Operation("Users", "Listar usuarios", "Apenas Administrador. Lista usuarios do diretorio LDAP demo sem expor senhas.", responseSchema: "UserDto[]") },
                ["/api/users/{userName}/role"] = new { put = Operation("Users", "Alterar papel", "Apenas Administrador. Atualiza o papel do usuario no diretorio centralizado. A mudanca vale no proximo login.", "UpdateRoleRequestDto", "MessageResponseDto", parameters: true, parameterName: "userName") },
                ["/api/audit"] = new { get = Operation("Audit", "Consultar auditoria", "Admin, Gestor ou Auditor. Retorna eventos recentes de login, documentos, papeis e exportacoes.", responseSchema: "AuditRecordDto[]") },
                ["/api/oauth/token"] = new { post = Operation("OAuth2 M2M", "Emitir token M2M", "Cliente M2M. Recebe client_id e client_secret, valida credenciais e emite token Bearer com escopo exports.m2m.", "ClientCredentialsRequestDto", "TokenResponseDto", security: false) },
                ["/api/m2m/export/{id}"] = new { post = Operation("OAuth2 M2M", "Exportar por M2M", "Requer Bearer token M2M com escopo exports.m2m. Exporta documento para storage externo local.", responseSchema: "ExportResultDto", bearer: true, parameters: true) }
            },
            ["components"] = Components()
        });
    }

    private static Dictionary<string, object?> Operation(
        string tag,
        string summary,
        string description,
        string? requestSchema = null,
        string? responseSchema = null,
        bool security = true,
        bool bearer = false,
        bool parameters = false,
        string parameterName = "id")
    {
        var operation = new Dictionary<string, object?>
        {
            ["tags"] = new[] { tag },
            ["summary"] = summary,
            ["description"] = description,
            ["responses"] = new Dictionary<string, object?>
            {
                ["200"] = new
                {
                    description = responseSchema is null ? "Operacao executada com sucesso." : $"Retorna {responseSchema}.",
                    content = responseSchema is null ? null : JsonContent(responseSchema)
                },
                ["401"] = new { description = "Nao autenticado ou token invalido." },
                ["403"] = new { description = "Autenticado, mas sem permissao RBAC para a rota." }
            }
        };

        if (security)
        {
            operation["security"] = new[] { bearer ? new Dictionary<string, string[]> { ["bearerAuth"] = Array.Empty<string>() } : new Dictionary<string, string[]> { ["cookieAuth"] = Array.Empty<string>() } };
        }

        if (requestSchema is not null)
        {
            operation["requestBody"] = new
            {
                required = true,
                content = requestSchema == "UploadDocumentRequestDto"
                    ? new Dictionary<string, object?> { ["multipart/form-data"] = new { schema = Ref(requestSchema) } }
                    : JsonContent(requestSchema)
            };
        }

        if (parameters)
        {
            operation["parameters"] = new[]
            {
                new
                {
                    name = parameterName,
                    @in = "path",
                    required = true,
                    schema = new { type = parameterName == "id" ? "integer" : "string" },
                    description = parameterName == "id" ? "Identificador do documento." : "Nome do usuario no diretorio."
                }
            };
        }

        return operation;
    }

    private static object JsonContent(string schemaName) => new Dictionary<string, object?>
    {
        ["application/json"] = new { schema = Ref(schemaName) }
    };

    private static object Ref(string schemaName)
    {
        if (schemaName.EndsWith("[]", StringComparison.Ordinal))
        {
            return new
            {
                type = "array",
                items = Ref(schemaName[..^2])
            };
        }

        if (schemaName == "arquivo binario")
        {
            return new { type = "string", format = "binary" };
        }

        return new Dictionary<string, object?>
        {
            ["$ref"] = $"#/components/schemas/{schemaName}"
        };
    }

    private static object Components() => new
    {
        securitySchemes = new Dictionary<string, object?>
        {
            ["cookieAuth"] = new { type = "apiKey", @in = "cookie", name = "DocumentPortalIam.Auth", description = "Cookie criado pelo login LDAP." },
            ["bearerAuth"] = new { type = "http", scheme = "bearer", description = "Token OAuth2 M2M emitido em /api/oauth/token." }
        },
        schemas = new Dictionary<string, object?>
        {
            ["LoginRequestDto"] = Schema(("userName", "string"), ("password", "string")),
            ["AuthenticatedUserDto"] = Schema(("userName", "string"), ("displayName", "string"), ("email", "string"), ("roles", "array"), ("permissions", "array")),
            ["UserDto"] = Schema(("userName", "string"), ("displayName", "string"), ("email", "string"), ("roles", "array")),
            ["DocumentDto"] = Schema(("id", "integer"), ("originalFileName", "string"), ("contentType", "string"), ("ownerUserName", "string"), ("sensitivity", "string"), ("uploadedAt", "string"), ("canDownload", "boolean"), ("canDelete", "boolean"), ("canExportOidc", "boolean")),
            ["UploadDocumentRequestDto"] = new { type = "object", properties = new Dictionary<string, object?> { ["file"] = new { type = "string", format = "binary" }, ["sensitivity"] = new { type = "string" } } },
            ["OidcExportRequestDto"] = Schema(("providerAccount", "string")),
            ["ExportResultDto"] = Schema(("protocol", "string"), ("actor", "string"), ("scope", "string"), ("documentId", "integer"), ("originalFileName", "string"), ("exportedAt", "string"), ("storedAt", "string"), ("metadata", "object")),
            ["ClientCredentialsRequestDto"] = Schema(("client_id", "string"), ("client_secret", "string")),
            ["TokenResponseDto"] = Schema(("access_token", "string"), ("token_type", "string"), ("expires_in", "integer"), ("scope", "string")),
            ["RoleDto"] = Schema(("name", "string"), ("description", "string"), ("permissions", "array")),
            ["UpdateRoleRequestDto"] = Schema(("role", "string")),
            ["AuditRecordDto"] = Schema(("timestamp", "string"), ("action", "string"), ("actor", "string"), ("details", "string")),
            ["MessageResponseDto"] = Schema(("message", "string"))
        }
    };

    private static object Schema(params (string Name, string Type)[] properties)
    {
        return new
        {
            type = "object",
            properties = properties.ToDictionary(
                item => item.Name,
                item => item.Type == "array"
                    ? (object)new { type = "array", items = new { type = "string" } }
                    : new { type = item.Type })
        };
    }
}
