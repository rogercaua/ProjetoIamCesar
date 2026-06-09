# Portal IAM Docs

Projeto pratico de Gestao de Identidade e Acesso em C# para um Sistema de Gestao de Documentos.

## O que foi implementado

- Login centralizado em servidor LDAP real, configurado em `appsettings.json`.
- RBAC com papeis, recursos e permissoes em `Back/Core/Services/RbacService.cs`.
- Controllers/API em `Back/Controllers`.
- Cookie Authentication para manter a sessao do usuario.
- OpenID Connect para conectar uma conta Google.
- Google Drive API para exportar documento para o Drive conectado.
- SQLite com EF Core para metadados dos documentos e logs de auditoria.
- Arquivos fisicos em `Storage/Documents`.
- OAuth2 Machine-to-Machine para exportacao tecnica.
- Swagger UI em `/swagger` para testar endpoints.
- Front estatico em HTML, CSS e Bootstrap simples, sem Razor.

## Configurar antes de rodar

Edite o `appsettings.json`:

```json
"Ldap": {
  "Host": "3.17.68.102",
  "Port": 389,
  "BaseDn": "dc=projetoiam,dc=local",
  "UsersOu": "ou=users",
  "GroupsOu": "ou=groups",
  "AdminDn": "cn=admin,dc=projetoiam,dc=local",
  "AdminPassword": "admin123"
}
```

Usuarios LDAP existentes:

| Usuario | Senha | Grupo LDAP | Papel na aplicacao |
|---|---|---|---|
| `admin` | `Admin@123` | `Administradores` | Administrador |
| `gestor` | `Gestor@123` | `Gestores` | Gestor |
| `aluno` | `Aluno@123` | `Usuarios` | Usuario |
| `auditor` | `Auditor@123` | `Auditores` | Auditor |

O projeto tambem deixa configurados os campos auxiliares:

```json
"Ldap": {
  "UseSsl": false,
  "UserSearchFilter": "(uid={0})",
  "UserNameAttribute": "uid",
  "DisplayNameAttribute": "cn",
  "EmailAttribute": "mail",
  "GroupNameAttribute": "cn",
  "GroupMemberAttribute": "member"
}
```

Para Google Drive, configure tambem:

```json
"Google": {
  "ClientId": "seu-client-id",
  "ClientSecret": "seu-client-secret"
}
```

No Google Cloud Console, use `http://localhost:5169/signin-google` como redirect URI.

## Como rodar

```powershell
dotnet restore
dotnet run --urls http://localhost:5169
```

Abra:

```text
http://localhost:5169
http://localhost:5169/swagger
```

## Fluxo de uso

1. Entre com usuario e senha existentes no LDAP do EC2.
2. Envie um documento pelo front.
3. Veja a listagem mudando conforme o papel RBAC.
4. Conecte uma conta Google.
5. Exporte o documento para Google Drive.
6. Teste os endpoints no Swagger.
7. Execute o fluxo M2M com token OAuth2.

## Fluxo M2M OAuth2

Com a aplicacao rodando e pelo menos um documento cadastrado:

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
- `Back/Core/Data/` - contexto EF Core/SQLite.
- `Back/Core/Dtos/` - objetos de entrada e saida da API.
- `Back/Core/Services/` - interfaces e implementacoes de LDAP, RBAC, documentos, auditoria e exportacoes.
- `Back/Core/Models/` - entidades do projeto.
- `Front/wwwroot/` - HTML, CSS e Bootstrap.
- `Storage/` - criada em tempo de execucao para banco SQLite, chaves, arquivos e exportacoes.
- `docs/` - diagrama, relatorio, roteiro de video e exemplo M2M.

## Principais controllers

| Controller | Rota base | Funcao |
|---|---|---|
| `AuthController` | `/api/auth` | Login LDAP, logout e dados da sessao |
| `GoogleController` | `/api/google` | Conectar/desconectar conta Google via OIDC |
| `DocumentsController` | `/api/documents` | Listar, enviar, baixar, excluir e exportar para Google Drive |
| `UsersController` | `/api/users` | Consultar usuarios LDAP e alterar papel |
| `RbacController` | `/api/rbac` | Consultar matriz de papeis |
| `AuditController` | `/api/audit` | Consultar eventos de auditoria no SQLite |
| `OAuthController` | `/api/oauth/token` | Emitir token M2M |
| `M2MController` | `/api/m2m/export/{id}` | Exportar documento com token Bearer |
