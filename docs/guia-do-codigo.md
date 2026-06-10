# Guia do Codigo

Este guia explica onde cada parte importante do projeto esta implementada. Use este arquivo para estudar antes da apresentacao.

## Entrada da aplicacao

### `Program.cs`

Responsavel por configurar a aplicacao.

Principais pontos:

- define `Front/wwwroot` como pasta publica do front;
- registra controllers;
- registra Swagger;
- configura SQLite com EF Core;
- configura Cookie Authentication;
- configura cookie externo do Google;
- configura OpenID Connect do Google;
- registra servicos do Core;
- cria o banco SQLite com `EnsureCreated`;
- mapeia controllers e fallback para `index.html`.

Trecho mais importante para IAM:

```text
AddAuthentication()
  AddCookie()
  AddCookie("GoogleExternal")
  AddOpenIdConnect("Google")
```

Isso mostra que existem dois cookies:

- `DocumentPortalIam.Auth`: sessao da aplicacao depois do login LDAP;
- `DocumentPortalIam.Google`: tokens da conta Google conectada.

## Controllers

Controllers ficam em `Back/Controllers`.

Eles nao devem guardar regra complexa. A funcao principal deles e:

1. receber a requisicao;
2. validar permissao;
3. chamar um servico;
4. devolver DTO ou redirect.

| Controller | Rota | Papel no projeto |
|---|---|---|
| `AuthController` | `/api/auth` | Login LDAP, logout e sessao atual |
| `DocumentsController` | `/api/documents` | Upload, listagem, download, exclusao e exportacao Drive |
| `GoogleController` | `/api/google` | Conectar, verificar e desconectar Google |
| `UsersController` | `/api/users` | Consultar usuarios LDAP e alterar papel |
| `RbacController` | `/api/rbac` | Expor matriz de roles e permissoes |
| `AuditController` | `/api/audit` | Consultar logs de auditoria |
| `OAuthController` | `/api/oauth` | Emitir token OAuth2 M2M |
| `M2MController` | `/api/m2m` | Exportar documento usando token Bearer |
| `PortalController` | `/dashboard` | Montar a tela HTML dinamica por papel |

## Core

O Core fica em `Back/Core`.

Ele concentra a parte que nao depende diretamente da tela.

### `Back/Core/Models`

Modelos principais:

| Arquivo | O que representa |
|---|---|
| `AppUser.cs` | Usuario vindo do LDAP |
| `DocumentRecord.cs` | Documento salvo no SQLite |
| `AuditRecord.cs` | Evento de auditoria |
| `RbacDefinition.cs` | Constantes de roles e permissoes |

### `Back/Core/Dtos`

DTOs sao objetos usados para entrada e saida da API.

Exemplos:

- `LoginRequestDto`: usuario e senha do login;
- `AuthenticatedUserDto`: usuario logado, roles e permissoes;
- `DocumentDto`: dados publicos de um documento;
- `UpdateRoleRequestDto`: novo papel de um usuario;
- `TokenResponseDto`: token M2M emitido.

DTO evita expor entidade interna diretamente e deixa o Swagger mais claro.

### `Back/Core/Data`

`AppDbContext.cs` configura as tabelas do SQLite:

- `Documents`;
- `AuditLogs`.

Nao existe tabela de usuarios no SQLite.

### `Back/Core/Services`

Interfaces e implementacoes das regras principais.

| Interface/Servico | Funcao |
|---|---|
| `IDirectoryService` / `LdapDirectoryService` | Autenticar e consultar usuarios no LDAP |
| `IRbacService` / `RbacService` | Mapear roles para permissoes e validar acesso |
| `IDocumentRepository` / `DocumentRepository` | Salvar metadados no SQLite e arquivo fisico no disco |
| `IAuditService` / `AuditService` | Gravar logs de auditoria no SQLite |
| `IGoogleDriveExportService` / `GoogleDriveExportService` | Exportar documento para Google Drive |
| `IM2MTokenService` / `M2MTokenService` | Emitir e validar token M2M |
| `IM2MStorageExportService` / `M2MStorageExportService` | Copiar arquivo para storage externo simulado |

## Fluxo de login LDAP no codigo

1. `Front/wwwroot/index.html` envia formulario para `/api/auth/login`.
2. `AuthController.Login` le usuario e senha.
3. `AuthController` chama `IDirectoryService.AuthenticateAsync`.
4. O DI do ASP.NET usa `LdapDirectoryService`.
5. `LdapDirectoryService.FindUserAsync` busca o usuario no LDAP.
6. `LdapDirectoryService` tenta bind com o DN do usuario e senha digitada.
7. Se der certo, busca grupos LDAP.
8. Grupos viram roles.
9. `AuthController` cria claims e cookie.
10. `AuditService` grava sucesso ou falha.

Frase para apresentar:

> A aplicacao nao confere senha no banco local. Ela faz bind no LDAP real da EC2. Se o LDAP recusar, o login falha.

## Fluxo RBAC no codigo

1. O LDAP retorna grupos.
2. Grupos viram roles da aplicacao.
3. As roles ficam no cookie como claims.
4. `RbacService.GetPermissions` transforma roles em permissoes.
5. Controllers chamam `HasPermission`, `CanViewDocument` ou `CanDownloadDocument`.
6. Se nao tiver permissao, a API retorna `Forbid`.
7. O `PortalController` tambem usa o RBAC para esconder partes da tela.

Exemplo pratico:

- `aluno` tem `documents.view.own`;
- por isso ele so ve documentos onde `OwnerUserName == aluno`;
- `gestor` tem `documents.view.all`;
- por isso ve documentos de todos;
- `auditor` nao tem permissao de documento;
- por isso nao aparece lista de arquivos para ele.

## Fluxo de documentos

### Upload

1. Tela envia arquivo para `POST /api/documents`.
2. `DocumentsController.Upload` exige `documents.upload`.
3. `DocumentRepository.SaveAsync` grava arquivo em `Storage/Documents`.
4. O mesmo metodo grava metadados no SQLite.
5. `AuditService` registra `document.upload`.

### Download

1. Usuario chama rota de download.
2. Controller busca metadado no SQLite.
3. `RbacService.CanDownloadDocument` decide se pode baixar.
4. Se puder, `DocumentRepository.OpenReadAsync` abre o arquivo fisico.
5. Auditoria registra `document.download`.

### Exclusao

1. Somente administrador tem `documents.delete`.
2. Controller remove metadado e arquivo fisico.
3. Auditoria registra `document.delete`.

## Fluxo Google

1. Usuario ja logado no LDAP clica em `Conectar Google`.
2. `GoogleController.Connect` chama `Challenge` do OIDC.
3. Google autentica e retorna para `/signin-google`.
4. O token fica no cookie `DocumentPortalIam.Google`.
5. Ao clicar em `Drive`, `DocumentsController` valida RBAC.
6. `GoogleDriveExportService` usa o access token.
7. A Google Drive API recebe o arquivo.

Frase para apresentar:

> O Google nao substitui o LDAP. Ele entra como provedor externo para autorizar a exportacao para Drive.

## Fluxo M2M

1. Cliente tecnico chama `/api/oauth/token`.
2. Envia `client_id` e `client_secret`.
3. `M2MTokenService` valida as credenciais.
4. A API devolve um Bearer token.
5. Cliente chama `/api/m2m/export/{id}`.
6. `M2MController` valida token e escopo.
7. `M2MStorageExportService` copia o arquivo para storage externo local.

Frase para apresentar:

> Esse fluxo representa uma integracao entre sistemas, sem usuario humano logado.

## Fluxo de troca de papel

1. Apenas administrador ve o painel de usuarios.
2. O painel lista usuarios consultando LDAP.
3. Admin escolhe usuario e novo papel.
4. `UsersController` valida `users.manage.roles`.
5. `LdapDirectoryService.UpdateRoleAsync` atualiza os grupos no LDAP.
6. A auditoria grava a troca.
7. O usuario faz novo login para receber o novo papel no cookie.

## O que o professor pode perguntar

### Onde esta a autenticacao?

No `AuthController` e no `LdapDirectoryService`.

### Onde esta a autorizacao?

No `RbacService` e nas validacoes dentro dos controllers.

### O que e governanca?

E a administracao de acesso. No projeto, isso aparece quando o administrador altera o papel do usuario no LDAP e essa mudanca afeta as permissoes da aplicacao.

### O que o banco guarda?

Documentos e auditoria. Nao guarda senha.

### Por que o auditor nao ve arquivos?

Porque ele so tem `audit.view`. Ele nao tem `documents.view.own` nem `documents.view.all`.

### Por que o gestor nao ve logs?

Porque ele nao tem `audit.view`. Ele gerencia documentos e exportacoes, mas nao auditoria.

### Como provar que o login bate no LDAP?

Mostrar:

- `appsettings.json` com host LDAP da EC2;
- `LdapDirectoryService` usando `BindAsync`;
- log `ldap.login.success`;
- trafego na EC2 com `tcpdump -ni any port 389`.
