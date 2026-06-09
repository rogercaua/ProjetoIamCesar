# Relatorio Tecnico - Portal IAM Docs

## Identificacao

Projeto: Sistema de Gestao de Documentos com IAM.

Integrantes: preencher com os nomes do grupo antes da entrega.

Linguagem e plataforma: C#, ASP.NET Core Web API, .NET 8, HTML, CSS e Bootstrap.

## Objetivo

O projeto implementa um portal simples de upload, download e exportacao de documentos com controles de identidade e acesso. O foco e demonstrar autenticacao centralizada, RBAC, OpenID Connect com Google, Google Drive API, OAuth2 M2M e governanca.

## Arquitetura

A aplicacao foi organizada em camadas simples:

- `Back/Controllers/`: rotas de API documentadas no Swagger.
- `Back/Core/Data/`: `AppDbContext` com EF Core e SQLite.
- `Back/Core/Dtos/`: objetos de entrada e saida usados pelos controllers.
- `Back/Core/Services/`: interfaces e implementacoes de LDAP, RBAC, documentos, auditoria e exportacoes.
- `Back/Core/Models/`: entidades de documento, auditoria, usuario e RBAC.
- `Front/wwwroot/`: front estatico com HTML, CSS e Bootstrap, sem Razor.

Os metadados dos documentos e os logs de auditoria ficam no SQLite em `Storage/iam-documents.db`. Os arquivos enviados ficam fisicamente em `Storage/Documents`.

## Autenticacao centralizada LDAP

A autenticacao esta em `Back/Core/Services/LdapDirectoryService.cs`, usando a biblioteca `Novell.Directory.Ldap.NETStandard`.

Fluxo logico:

1. O usuario informa login e senha no `Front/wwwroot/index.html`.
2. O front envia `POST /api/auth/login`.
3. O backend faz bind de servico no LDAP configurado no EC2.
4. O backend busca o usuario com `UserSearchFilter`, por exemplo `(uid={0})`.
5. O backend tenta bind com o DN do usuario e a senha informada.
6. Se o bind for valido, a aplicacao cria cookie de sessao.
7. A aplicacao busca os grupos do usuario em `ou=groups`.
8. Os grupos `Administradores`, `Gestores`, `Usuarios` e `Auditores` viram claims de role.
9. A auditoria grava o evento no SQLite.

## Modelagem RBAC

A matriz RBAC esta em `Back/Core/Services/RbacService.cs`. Os papeis sao:

| Papel | Finalidade |
|---|---|
| Administrador | Controle total, inclusive governanca de usuarios |
| Gestor | Gerencia documentos e exportacoes |
| Usuario | Opera apenas os proprios documentos |
| Auditor | Consulta apenas eventos de auditoria sem alterar dados |
| ServicoM2M | Conta tecnica para exportacao por API |

Permissoes principais:

- `documents.upload`
- `documents.view.own`
- `documents.view.all`
- `documents.download.own`
- `documents.download.all`
- `documents.delete`
- `exports.google_drive`
- `exports.m2m`
- `users.manage.roles`
- `audit.view`

A verificacao acontece antes de cada acao sensivel. Por exemplo, usuario comum so ve documentos cujo `OwnerUserName` seja igual ao nome da sessao.

## Governanca de acesso

O endpoint `PUT /api/users/{userName}/role` permite que apenas Administrador altere o papel do usuario no LDAP. A alteracao remove o DN do usuario dos grupos de papel e adiciona no grupo correspondente ao novo papel. A mudanca passa a valer no proximo login.

## OpenID Connect e Google Drive

A conexao Google esta em `Back/Controllers/GoogleController.cs`, usando `Microsoft.AspNetCore.Authentication.OpenIdConnect`.

Fluxo logico:

1. Usuario autenticado no LDAP chama `GET /api/google/connect`.
2. A aplicacao redireciona para o Google via OpenID Connect.
3. O Google retorna para `/signin-google`.
4. O token externo fica em um cookie separado chamado `DocumentPortalIam.Google`.
5. O usuario chama `POST /api/documents/{id}/export/google-drive`.
6. `GoogleDriveExportService` usa o access token e a Google Drive API para enviar o arquivo.
7. A auditoria registra a exportacao no SQLite.

## OAuth2 M2M

A API M2M esta em `Back/Controllers/OAuthController.cs`, `Back/Controllers/M2MController.cs`, `Back/Core/Services/M2MTokenService.cs` e `Back/Core/Services/M2MStorageExportService.cs`.

Fluxo logico:

1. Cliente tecnico chama `POST /api/oauth/token` com `client_id` e `client_secret`.
2. A aplicacao valida as credenciais configuradas em `appsettings.json`.
3. Um token Bearer com escopo `exports.m2m` e emitido por 30 minutos.
4. O cliente chama `POST /api/m2m/export/{id}` com o token.
5. O documento e copiado para `Storage/external/m2m-storage`.
6. A auditoria registra emissao do token e exportacao.

Credenciais M2M usadas no projeto:

- `client_id`: `storage-client`
- `client_secret`: `M2M@123`

## Auditoria

O servico `AuditService` grava eventos na tabela `AuditLogs` do SQLite. O endpoint `GET /api/audit` mostra eventos recentes para papeis com `audit.view`.

Eventos registrados:

- login LDAP com sucesso ou falha
- logout
- upload
- download
- exclusao
- troca de papel
- emissao de token M2M
- exportacao Google Drive
- exportacao M2M

## Bibliotecas utilizadas

- `Microsoft.AspNetCore.Authentication.Cookies`: sessao autenticada por cookie.
- `Microsoft.AspNetCore.Authentication.OpenIdConnect`: conexao Google via OIDC.
- `Novell.Directory.Ldap.NETStandard`: autenticacao e consulta LDAP real.
- `Microsoft.EntityFrameworkCore.Sqlite`: SQLite para documentos e auditoria.
- `Google.Apis.Drive.v3`: exportacao para Google Drive.
- `Swashbuckle.AspNetCore`: Swagger UI para testar endpoints.
- `System.Security.Cryptography`: geracao de token aleatorio para M2M.

## Como demonstrar

1. Rodar a aplicacao com `dotnet run --urls http://localhost:5169`.
2. Entrar com usuario LDAP do EC2.
3. Enviar um documento.
4. Mostrar que o RBAC muda listagem/acoes conforme o papel.
5. Conectar Google por OIDC.
6. Exportar documento para Google Drive.
7. Executar o fluxo M2M pelo PowerShell.
8. Abrir `/swagger` e testar endpoints.
9. Abrir auditoria e mostrar os eventos gravados no SQLite.
