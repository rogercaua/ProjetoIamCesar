using System.Net;
using System.Security.Claims;
using System.Text;
using DocumentPortalIam.Back.Core.Models;
using DocumentPortalIam.Back.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentPortalIam.Back.Controllers;

[Authorize]
public sealed class PortalController : Controller
{
    private readonly IDocumentRepository _documents;
    private readonly IDirectoryService _directory;
    private readonly IRbacService _rbac;
    private readonly IAuditService _audit;

    public PortalController(
        IDocumentRepository documents,
        IDirectoryService directory,
        IRbacService rbac,
        IAuditService audit)
    {
        _documents = documents;
        _directory = directory;
        _rbac = rbac;
        _audit = audit;
    }

    [HttpGet("/dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var permissions = _rbac.GetPermissions(User);
        var canSeeDocuments = CanSeeDocuments();
        var documents = canSeeDocuments
            ? (await _documents.GetAllAsync())
                .Where(document => _rbac.CanViewDocument(User, document))
                .ToList()
            : new List<DocumentRecord>();

        var google = await HttpContext.AuthenticateAsync("GoogleExternal");
        var googleName = google.Principal?.FindFirstValue(ClaimTypes.Email)
            ?? google.Principal?.FindFirstValue(ClaimTypes.Name)
            ?? "";

        var html = new StringBuilder();
        html.AppendLine("""
        <!doctype html>
        <html lang="pt-br">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Portal IAM Docs - Painel</title>
            <link rel="stylesheet" href="/lib/bootstrap/dist/css/bootstrap.min.css">
            <link rel="stylesheet" href="/css/site.css?v=20260609-3">
        </head>
        <body>
        """);

        html.AppendLine(RenderHeader());
        html.AppendLine("""<main class="container page-shell py-5">""");
        html.AppendLine(RenderHeading(permissions));
        html.AppendLine(RenderPrimaryActions(google.Succeeded, googleName));
        html.AppendLine(RenderDocuments(documents));
        html.AppendLine(await RenderUsersPanel());
        html.AppendLine(await RenderAuditPanel());
        html.AppendLine("</main></body></html>");

        return Content(html.ToString(), "text/html; charset=utf-8");
    }

    private string RenderHeader()
    {
        return $"""
        <header class="topbar bg-white border-bottom">
            <div class="container topbar-inner d-flex align-items-center justify-content-between py-3">
                <a class="brand" href="/dashboard">Portal IAM Docs</a>
                <nav class="nav-actions">
                    <span class="session-chip">{H(User.Identity?.Name ?? "usuario")}</span>
                    <a class="nav-link-clean" href="/swagger">Swagger</a>
                    <form action="/api/auth/logout" method="post">
                        <button class="btn-link-clean" type="submit">Sair</button>
                    </form>
                </nav>
            </div>
        </header>
        """;
    }

    private string RenderHeading(IReadOnlyList<string> permissions)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList();
        return $"""
        <section class="page-heading">
            <p class="eyebrow">Painel operacional</p>
            <h1>Documentos, acessos e exportacoes</h1>
            <p class="muted mb-0">Papel: {H(string.Join(", ", roles))}</p>
            <p class="muted small-note">Permissoes: {H(string.Join(", ", permissions))}</p>
        </section>
        """;
    }

    private string RenderPrimaryActions(bool googleConnected, string googleName)
    {
        var cards = new List<string>();

        if (_rbac.HasPermission(User, Permissions.UploadDocument))
        {
            var uploadCard = RenderUploadCard();
            if (!string.IsNullOrWhiteSpace(uploadCard))
            {
                cards.Add(uploadCard);
            }
        }

        if (_rbac.HasPermission(User, Permissions.ExportGoogleDrive))
        {
            cards.Add(RenderGoogleCard(googleConnected, googleName));
        }

        if (cards.Count == 0)
        {
            return "";
        }

        var gridClass = cards.Count == 1 ? "grid-1" : "grid-2";
        return $"""<section class="{gridClass} section-gap">{string.Join(Environment.NewLine, cards)}</section>""";
    }

    private string RenderUploadCard()
    {
        var allowedSensitivities = _rbac.GetAllowedUploadSensitivities(User);
        if (allowedSensitivities.Count == 0)
        {
            return "";
        }

        var options = string.Join(Environment.NewLine, allowedSensitivities.Select(sensitivity =>
            $"""<option value="{H(sensitivity)}">{H(sensitivity)}</option>"""));

        return $"""
            <form class="panel bg-white border rounded-3 p-4 shadow-sm" action="/api/documents" method="post" enctype="multipart/form-data">
                <h2>Enviar documento</h2>
                <label for="file">Arquivo</label>
                <input id="file" name="file" class="form-control" type="file" required>

                <label for="sensitivity">Classificacao</label>
                <select id="sensitivity" name="sensitivity" class="form-select">
                    {options}
                </select>

                <button class="btn-main" type="submit">Enviar</button>
            </form>
        """;
    }

    private static string RenderGoogleCard(bool googleConnected, string googleName)
    {
        var googleText = googleConnected
            ? $"Conectado como {H(googleName)}"
            : "Nenhuma conta Google conectada.";
        var googleActions = googleConnected
            ? """
            <a class="btn-secondary-clean" href="/api/google/status">Ver status</a>
            """
            : """
            <a class="btn-main as-link" href="/api/google/connect?returnUrl=/dashboard">Conectar Google</a>
            <a class="btn-secondary-clean" href="/api/google/status">Ver status</a>
            """;
        var disconnectForm = googleConnected
            ? """
            <form action="/api/google/disconnect" method="post" class="inline-form">
                <button class="btn-secondary-clean" type="submit">Desconectar Google</button>
            </form>
            """
            : "";

        return $"""
        <div class="panel bg-white border rounded-3 p-4 shadow-sm">
            <h2>Google Drive</h2>
            <p class="muted">{googleText}</p>
            <div class="button-row">
                {googleActions}
            </div>
            {disconnectForm}
        </div>
        """;
    }

    private string RenderDocuments(IReadOnlyList<DocumentRecord> documents)
    {
        if (!CanSeeDocuments())
        {
            return "";
        }

        var rows = new StringBuilder();
        foreach (var document in documents)
        {
            rows.AppendLine($"""
            <tr>
                <td class="fw-semibold">#{document.Id}</td>
                <td>
                    <div class="document-title">{H(document.OriginalFileName)}</div>
                    <div class="document-meta">{FormatBytes(document.SizeInBytes)} - {document.UploadedAt.ToLocalTime():dd/MM/yyyy HH:mm}</div>
                </td>
                <td>{H(document.OwnerUserName)}</td>
                <td><span class="role-pill">{H(document.Sensitivity)}</span></td>
                <td class="actions-cell">
                    {RenderDocumentActions(document)}
                </td>
            </tr>
            """);
        }

        if (documents.Count == 0)
        {
            rows.AppendLine("""<tr><td colspan="5" class="text-secondary">Nenhum documento encontrado para o seu papel.</td></tr>""");
        }

        return $"""
        <section class="panel bg-white border rounded-3 p-4 shadow-sm section-gap">
            <div class="section-title-row">
                <div>
                    <h2>Arquivos no banco</h2>
                    <p class="muted mb-0">Metadados vindos do SQLite; arquivos fisicos ficam em Storage/Documents.</p>
                </div>
                <a class="btn-secondary-clean" href="/api/documents">Ver JSON</a>
            </div>
            <div class="table-responsive">
                <table class="table table-hover align-middle document-table">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Arquivo</th>
                            <th>Dono</th>
                            <th>Classificacao</th>
                            <th>Acoes</th>
                        </tr>
                    </thead>
                    <tbody>{rows}</tbody>
                </table>
            </div>
        </section>
        """;
    }

    private string RenderDocumentActions(DocumentRecord document)
    {
        var actions = new StringBuilder();

        if (_rbac.CanDownloadDocument(User, document))
        {
            actions.Append($"""<a class="btn-secondary-clean btn-small" href="/api/documents/{document.Id}/download">Baixar</a>""");
        }

        if (_rbac.HasPermission(User, Permissions.ExportGoogleDrive))
        {
            actions.Append($"""
            <form action="/api/documents/export/google-drive" method="post" class="inline-action-form">
                <input type="hidden" name="id" value="{document.Id}">
                <button class="btn-secondary-clean btn-small" type="submit">Drive</button>
            </form>
            """);
        }

        if (_rbac.HasPermission(User, Permissions.DeleteDocuments))
        {
            actions.Append($"""
            <form action="/api/documents/delete" method="post" class="inline-action-form">
                <input type="hidden" name="id" value="{document.Id}">
                <button class="btn-danger-inline" type="submit">Excluir</button>
            </form>
            """);
        }

        return actions.Length == 0 ? """<span class="muted">Sem acoes</span>""" : actions.ToString();
    }

    private async Task<string> RenderUsersPanel()
    {
        if (!_rbac.HasPermission(User, Permissions.ManageRoles))
        {
            return "";
        }

        try
        {
            var users = await _directory.GetUsersAsync();
            var userOptions = string.Join(Environment.NewLine, users.Select(user =>
                $"""<option value="{H(user.UserName)}">{H(user.DisplayName)} ({H(user.UserName)}) - {H(string.Join(", ", user.Roles))}</option>"""));

            var roleOptions = string.Join(Environment.NewLine, _rbac.Roles
                .Where(role => role.Name != AppRoles.Service)
                .Select(role => $"""<option value="{H(role.Name)}">{H(role.Name)}</option>"""));

            return $"""
            <section class="grid-2 section-gap">
                <form class="panel bg-white border rounded-3 p-4 shadow-sm" action="/api/users/role" method="post">
                    <h2>Alterar papel LDAP</h2>
                    <label for="userName">Usuario LDAP</label>
                    <select id="userName" name="userName" class="form-select" required>
                        {userOptions}
                    </select>

                    <label for="role">Novo papel</label>
                    <select id="role" name="role" class="form-select" required>
                        {roleOptions}
                    </select>

                    <button class="btn-main" type="submit">Atualizar papel</button>
                    <p class="muted small-note">O usuario precisa fazer novo login para receber o novo papel.</p>
                </form>

                <div class="panel bg-white border rounded-3 p-4 shadow-sm">
                    <h2>Usuarios disponiveis</h2>
                    <div class="user-list">
                        {RenderUserCards(users)}
                    </div>
                </div>
            </section>
            """;
        }
        catch (Exception exception)
        {
            return $"""
            <section class="panel bg-white border rounded-3 p-4 shadow-sm section-gap">
                <h2>Usuarios LDAP</h2>
                <p class="text-danger mb-0">Nao foi possivel consultar o LDAP: {H(exception.Message)}</p>
            </section>
            """;
        }
    }

    private static string RenderUserCards(IReadOnlyList<AppUser> users)
    {
        if (users.Count == 0)
        {
            return """<p class="muted mb-0">Nenhum usuario retornado pelo LDAP.</p>""";
        }

        return string.Join(Environment.NewLine, users.Select(user => $"""
        <div class="user-card">
            <div class="fw-semibold">{H(user.DisplayName)} <span class="muted">({H(user.UserName)})</span></div>
            <div class="document-meta">{H(user.Email)}</div>
            <div class="role-pill">{H(string.Join(", ", user.Roles))}</div>
        </div>
        """));
    }

    private async Task<string> RenderAuditPanel()
    {
        if (!_rbac.HasPermission(User, Permissions.ViewAudit))
        {
            return "";
        }

        var records = await _audit.GetRecentAsync(12);
        var items = records.Count == 0
            ? """<p class="muted mb-0">Nenhum evento de auditoria encontrado.</p>"""
            : string.Join(Environment.NewLine, records.Select(record => $"""
                <div class="audit-item">
                    <div class="fw-semibold">{H(record.Action)} <span class="muted">por {H(record.Actor)}</span></div>
                    <div>{H(record.Details)}</div>
                    <div class="audit-meta">{record.Timestamp.ToLocalTime():dd/MM/yyyy HH:mm}</div>
                </div>
            """));

        return $"""
        <section class="panel bg-white border rounded-3 p-4 shadow-sm section-gap">
            <div class="section-title-row">
                <div>
                    <h2>Auditoria recente</h2>
                    <p class="muted mb-0">Eventos gravados no SQLite.</p>
                </div>
                <a class="btn-secondary-clean" href="/api/audit">Ver JSON</a>
            </div>
            <div class="audit-list">{items}</div>
        </section>
        """;
    }

    private static string FormatBytes(long value)
    {
        if (value < 1024)
        {
            return $"{value} B";
        }

        if (value < 1024 * 1024)
        {
            return $"{value / 1024d:0.0} KB";
        }

        return $"{value / 1024d / 1024d:0.0} MB";
    }

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? "");

    private bool CanSeeDocuments()
    {
        return _rbac.CanAccessDocuments(User);
    }
}
