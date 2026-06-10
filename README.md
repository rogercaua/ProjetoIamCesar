# Portal IAM Docs

Sistema de Gestao de Documentos com IAM, feito em C# com ASP.NET Core Web API.

O objetivo do projeto e demonstrar, de forma pratica, autenticacao centralizada por LDAP, autorizacao por RBAC, conexao Google por OpenID Connect, exportacao para Google Drive, autorizacao OAuth2 Machine-to-Machine e auditoria.

## Resumo rapido

| Item | Implementacao no projeto |
|---|---|
| Login principal | LDAP real em uma EC2 |
| Sessao | Cookie Authentication do ASP.NET Core |
| Autorizacao | RBAC em `Back/Core/Services/RbacService.cs` |
| Banco de dados | SQLite apenas para documentos e auditoria |
| Arquivos fisicos | `Storage/Documents` |
| Google | OpenID Connect para conectar conta e Google Drive API para exportar arquivo |
| M2M | Token OAuth2 proprio para exportacao tecnica |
| Front | HTML, CSS e Bootstrap, sem Razor |
| Testes de endpoint | Swagger em `/swagger` |

Importante: o login nao usa SQLite. O banco guarda metadados de documentos e logs de auditoria. Quem autentica o usuario e define o grupo inicial e o LDAP.

## Como a aplicacao esta organizada

```text
Back/
  Controllers/        Endpoints HTTP da API
  Core/
    Data/             AppDbContext do EF Core/SQLite
    Dtos/             Objetos de entrada e saida dos endpoints
    Models/           Entidades e constantes de RBAC
    Services/         Interfaces e regras de negocio

Front/
  wwwroot/
    index.html        Tela de login estatica
    css/site.css      Estilo do front
    lib/bootstrap/    Bootstrap local

Storage/
  iam-documents.db    Banco SQLite criado em execucao
  Documents/          Arquivos enviados
  external/           Exportacoes M2M

docs/
  relatorio-tecnico.md
  guia-do-codigo.md
  diagrama-rbac.md
  fluxograma-rbac.md
  roteiro-video.md
  demo-m2m.ps1
```

O painel `/dashboard` e montado pelo `PortalController` em HTML puro. Isso foi feito para manter o projeto sem Razor e sem JavaScript, mas ainda permitir que a tela mostre dados reais vindos do SQLite e do LDAP.

## Requisitos atendidos

| Requisito | Onde esta no codigo | Como demonstrar |
|---|---|---|
| RBAC | `RbacService.cs`, `RbacDefinition.cs` e controllers | Entrar com usuarios diferentes e ver telas/acoes diferentes |
| LDAP real | `LdapDirectoryService.cs` | Fazer login com usuarios do servidor EC2 |
| OIDC Google | `Program.cs` e `GoogleController.cs` | Clicar em `Conectar Google` |
| Google Drive API | `GoogleDriveExportService.cs` | Exportar documento pelo botao `Drive` |
| OAuth2 M2M | `OAuthController.cs`, `M2MController.cs` e `M2MTokenService.cs` | Executar `docs/demo-m2m.ps1` |
| SQLite | `AppDbContext.cs`, `DocumentRepository.cs` e `AuditService.cs` | Mostrar `Storage/iam-documents.db` |
| Auditoria | `AuditService.cs` e `AuditController.cs` | Abrir a tela de auditoria como admin/auditor |
| Swagger | `Program.cs` e annotations nos controllers | Abrir `/swagger` |

## Usuarios LDAP

| Usuario | Senha | Grupo LDAP | Papel na aplicacao |
|---|---|---|---|
| `admin` | `Admin@123` | `Administradores` | Administrador |
| `gestor` | `Gestor@123` | `Gestores` | Gestor |
| `aluno` | `Aluno@123` | `Usuarios` | Usuario |
| `auditor` | `Auditor@123` | `Auditores` | Auditor |

Configuracao usada em `appsettings.json`:

```json
"Ldap": {
  "Host": "3.17.68.102",
  "Port": 389,
  "UseSsl": false,
  "BaseDn": "dc=projetoiam,dc=local",
  "UsersOu": "ou=users",
  "GroupsOu": "ou=groups",
  "AdminDn": "cn=admin,dc=projetoiam,dc=local",
  "AdminPassword": "admin123",
  "UserSearchFilter": "(uid={0})"
}
```

## Permissoes por papel

| Papel | O que ve na tela | O que pode fazer |
|---|---|---|
| Administrador | Documentos, upload, Google Drive, usuarios LDAP e auditoria | Controle total |
| Gestor | Documentos, upload e Google Drive | Ver/baixar todos os documentos e exportar para Drive |
| Usuario | Upload, Google Drive e apenas os proprios documentos | Enviar, ver/baixar proprios e exportar proprios |
| Auditor | Auditoria | Ver logs sem alterar dados |
| ServicoM2M | Sem tela de usuario | Exportar por token Bearer M2M |

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

## Configurar Google

No Google Cloud Console:

1. Ative a Google Drive API.
2. Configure a tela de consentimento OAuth.
3. Crie uma credencial do tipo `Aplicativo da Web`.
4. Cadastre o redirect URI:

```text
http://localhost:5169/signin-google
```

Depois preencha em `appsettings.json`:

```json
"Google": {
  "ClientId": "seu-client-id",
  "ClientSecret": "seu-client-secret"
}
```

O campo `GoogleDrive:UploadFolderId` pode ficar vazio. Se ficar vazio, o arquivo vai para a raiz do Drive da conta conectada.

## Fluxo normal de demonstracao

1. Rodar a aplicacao.
2. Entrar como `admin`.
3. Mostrar que o login foi feito pelo LDAP.
4. Enviar um documento.
5. Mostrar que os metadados foram salvos no SQLite.
6. Entrar como `aluno` e mostrar que ele so ve os proprios documentos.
7. Entrar como `auditor` e mostrar que ele so ve auditoria.
8. Entrar como `gestor` e mostrar que ele ve documentos, mas nao ve logs.
9. Conectar Google e exportar um documento para o Drive.
10. Executar o fluxo M2M.
11. Abrir Swagger e mostrar os endpoints documentados.

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

Credenciais M2M:

| Campo | Valor |
|---|---|
| `client_id` | `storage-client` |
| `client_secret` | `M2M@123` |
| `scope` | `exports.m2m` |

## Como provar que o login e LDAP

No codigo:

- `AuthController` chama `IDirectoryService.AuthenticateAsync`.
- A implementacao registrada no `Program.cs` e `LdapDirectoryService`.
- `LdapDirectoryService` usa `Novell.Directory.Ldap.NETStandard`.
- O SQLite nao possui tabela de usuarios nem senha.

Na demonstracao:

- Alterar a senha ou grupo no LDAP muda o comportamento do login.
- O log de auditoria grava `ldap.login.success` e `ldap.login.failed`.
- No servidor EC2, e possivel acompanhar conexoes na porta 389.

Comandos uteis no servidor EC2:

```bash
sudo tcpdump -ni any port 389
```

Em servidores Ubuntu/Debian, os logs do OpenLDAP normalmente aparecem em:

```bash
sudo tail -f /var/log/syslog
```

Em Amazon Linux/RHEL, geralmente:

```bash
sudo tail -f /var/log/messages
```

## Documentos principais

- `docs/guia-do-codigo.md`: explicacao arquivo por arquivo.
- `docs/relatorio-tecnico.md`: texto formal para entrega.
- `docs/diagrama-rbac.md`: matriz RBAC em Mermaid.
- `docs/fluxograma-rbac.md`: fluxo de decisao do RBAC.
- `docs/roteiro-video.md`: roteiro de demonstracao.
