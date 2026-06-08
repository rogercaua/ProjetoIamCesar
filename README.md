# Portal IAM Docs

Projeto pratico de Gestao de Identidade e Acesso em C# para um Sistema de Gestao de Documentos.

## O que foi implementado

- Login centralizado por LDAP demo em `Data/demo-ldap-users.json`.
- RBAC com papeis, recursos e permissoes em `Back/Core/Services/RbacService.cs`.
- Back-end organizado com `Back/Controllers` e `Back/Core`.
- Front-end separado em `Front/Pages` e `Front/wwwroot`.
- Swagger/OpenAPI documentado em `/swagger` e `/swagger/v1/swagger.json`.
- Upload, listagem, download e exclusao de documentos.
- Governanca simples: administrador altera o papel do usuario no diretorio.
- Exportacao OIDC demo para "Google Drive" local.
- API OAuth2 Machine-to-Machine com client credentials para exportar documento para storage externo local.
- Auditoria de login, upload, download, troca de papel e exportacoes.

## Como rodar

```powershell
dotnet restore
dotnet run --urls http://localhost:5169
```

Abra `http://localhost:5169`.

Swagger:

```text
http://localhost:5169/swagger
http://localhost:5169/swagger/v1/swagger.json
```

## Usuarios de demonstracao

| Usuario | Senha | Papel inicial |
|---|---|---|
| admin | Admin@123 | Administrador |
| gestor | Gestor@123 | Gestor |
| aluno | Aluno@123 | Usuario |
| auditor | Auditor@123 | Auditor |

## Fluxo M2M OAuth2

Com a aplicacao rodando e pelo menos um documento cadastrado, execute:

```powershell
$token = Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5169/api/oauth/token" `
  -ContentType "application/x-www-form-urlencoded" `
  -Body "client_id=storage-client&client_secret=M2M@123"

Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5169/api/m2m/export/1" `
  -Headers @{ Authorization = "Bearer $($token.access_token)" }
```

## Pastas importantes

- `Back/Controllers/` - endpoints da API usando DTOs.
- `Back/Core/Dtos/` - objetos de entrada e saida da API.
- `Back/Core/Services/` - interfaces e implementacoes das regras IAM, diretorio, auditoria e exportacoes.
- `Back/Core/Models/` - entidades simples do projeto.
- `Front/Pages/` - telas Razor Pages.
- `Front/wwwroot/` - CSS, JS e bibliotecas estaticas do front.
- `Storage/` - criada em tempo de execucao para documentos, auditoria e exportacoes.
- `docs/` - diagrama, relatorio, roteiro de video e exemplo M2M.

## Observacao sobre o LDAP demo

Para manter o projeto simples e executavel em qualquer maquina, o LDAP foi representado por um diretorio local em JSON. A classe `DemoLdapDirectoryService` centraliza autenticacao e papeis, como um servidor LDAP faria em laboratorio. Em uma implantacao real, esta classe seria trocada por um bind LDAP usando servidor corporativo.

## Principais controllers

| Controller | Rota base | Funcao |
|---|---|---|
| `AuthController` | `/api/auth` | Login, logout e dados da sessao |
| `DocumentsController` | `/api/documents` | Listar, enviar, baixar, excluir e exportar OIDC |
| `UsersController` | `/api/users` | Consultar usuarios e alterar papel |
| `RbacController` | `/api/rbac` | Consultar matriz de papeis |
| `AuditController` | `/api/audit` | Consultar eventos de auditoria |
| `OAuthController` | `/api/oauth/token` | Emitir token M2M |
| `M2MController` | `/api/m2m/export/{id}` | Exportar documento com token Bearer |
