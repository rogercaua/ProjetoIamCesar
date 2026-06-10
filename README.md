# Portal IAM Docs

Sistema de Gestao de Documentos com IAM, desenvolvido em C# com ASP.NET Core Web API.

O projeto demonstra, de forma pratica, os principais conceitos de Gestao de Identidade e Acesso:

- autenticacao centralizada por LDAP real em uma EC2;
- autorizacao por RBAC;
- sessao local com Cookie Authentication;
- conexao Google por OpenID Connect;
- exportacao para Google Drive API;
- autorizacao Machine-to-Machine com OAuth2;
- SQLite para metadados e auditoria;
- front em HTML, CSS e Bootstrap, sem Razor.

## Resumo do Projeto

| Area | Implementacao |
|---|---|
| Login principal | LDAP real na AWS EC2 |
| Sessao do usuario | Cookie Authentication |
| Autorizacao | RBAC no back-end |
| Banco de dados | SQLite |
| Arquivos enviados | `Storage/Documents` |
| Auditoria | Tabela `AuditLogs` no SQLite |
| Google | OpenID Connect + Google Drive API |
| M2M | OAuth2 Client Credentials com Bearer token |
| Front-end | HTML, CSS e Bootstrap |
| Teste de API | Swagger em `/swagger` |

Importante: o SQLite nao autentica usuario. O login e feito no LDAP. O banco local guarda apenas documentos e logs de auditoria.

## Estrutura de Pastas

```text
Back/
  Controllers/        Endpoints HTTP da API
  Core/
    Data/             AppDbContext do EF Core/SQLite
    Dtos/             Objetos de entrada e saida da API
    Models/           Entidades e constantes de RBAC
    Services/         Interfaces e regras de negocio

Front/
  wwwroot/
    index.html        Tela de login estatica
    dashboard.html    Redirecionamento para o painel
    css/site.css      Estilos do front
    lib/bootstrap/    Bootstrap local

Storage/
  iam-documents.db    Banco SQLite criado em execucao
  Documents/          Arquivos enviados
  external/           Exportacoes M2M

docs/
  diagrama-rbac.md
  diagrama-rbac.mmd
  fluxograma-rbac.md
  fluxograma-rbac.mmd
  guia-do-codigo.md
  relatorio-tecnico.md
  roteiro-video.md
  roteiro-slides-canva.md
  demo-m2m.ps1
```

O login fica em HTML estatico. O painel `/dashboard` e montado pelo `PortalController` em HTML puro para mostrar dados reais do SQLite e do LDAP sem Razor e sem JavaScript.

## Requisitos Atendidos

| Requisito | Onde esta no codigo | Como demonstrar |
|---|---|---|
| RBAC | `Back/Core/Services/RbacService.cs` e `Back/Core/Models/RbacDefinition.cs` | Entrar com usuarios diferentes e comparar telas/permissoes |
| LDAP real | `Back/Core/Services/LdapDirectoryService.cs` | Logar com usuarios do servidor EC2 |
| OIDC Google | `Program.cs` e `Back/Controllers/GoogleController.cs` | Clicar em `Conectar Google` |
| Google Drive API | `Back/Core/Services/GoogleDriveExportService.cs` | Exportar documento pelo botao `Drive` |
| OAuth2 M2M | `OAuthController`, `M2MController` e `M2MTokenService` | Executar `docs/demo-m2m.ps1` |
| SQLite | `AppDbContext`, `DocumentRepository` e `AuditService` | Mostrar `Storage/iam-documents.db` |
| Auditoria | `AuditService` e `AuditController` | Entrar como admin ou auditor |
| Swagger | `Program.cs` e annotations nos controllers | Abrir `/swagger` |

## Usuarios LDAP

| Usuario | Senha | Grupo LDAP | Papel RBAC |
|---|---|---|---|
| `admin` | `Admin@123` | `Administradores` | Administrador |
| `gestor` | `Gestor@123` | `Gestores` | Gestor |
| `aluno` | `Aluno@123` | `Usuarios` | Usuario |
| `auditor` | `Auditor@123` | `Auditores` | Auditor |

Configuracao LDAP usada em `appsettings.json`:

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

## Regras de Acesso

| Papel | Tela liberada | Permissao real no back-end |
|---|---|---|
| Administrador | Documentos, upload, Google Drive, usuarios LDAP e auditoria | Controle total |
| Gestor | Documentos, upload e Google Drive | Envia/ver/baixa todos os documentos |
| Usuario | Upload, Google Drive e documentos publicos | Envia apenas `Publico`, ve/baixa apenas `Publico` |
| Auditor | Auditoria | Ve apenas logs |
| ServicoM2M | Sem tela | Exporta por token Bearer M2M |

Regra importante:

- `Usuario/aluno` nao ve documento `Interno` ou `Confidencial`.
- `Usuario/aluno` nao consegue enviar documento `Interno` ou `Confidencial`.
- Mesmo que tente pelo Swagger/API, o back-end retorna acesso negado.
- `Auditor` nao acessa documentos.
- `Gestor` nao acessa auditoria nem troca papeis.

## Como Rodar

```powershell
dotnet restore
dotnet run --urls http://localhost:5169
```

Depois abra:

```text
http://localhost:5169
http://localhost:5169/swagger
```

## Configurar Google OIDC

No Google Cloud Console:

1. Ative a Google Drive API.
2. Configure a tela de consentimento OAuth.
3. Crie uma credencial do tipo `Aplicativo da Web`.
4. Cadastre a origem JavaScript:

```text
http://localhost:5169
```

5. Cadastre o redirect URI:

```text
http://localhost:5169/signin-google
```

6. Em modo de teste, adicione seu Gmail em `Usuarios de teste`.
7. Preencha o `appsettings.json`:

```json
"Google": {
  "ClientId": "seu-client-id",
  "ClientSecret": "seu-client-secret"
}
```

O campo `GoogleDrive:UploadFolderId` pode ficar vazio. Nesse caso, o arquivo sera enviado para a raiz do Drive da conta conectada.

Erros comuns:

| Erro | Causa provavel | Solucao |
|---|---|---|
| `redirect_uri_mismatch` | URI diferente da cadastrada | Cadastrar exatamente `http://localhost:5169/signin-google` |
| `access_denied` app em teste | Gmail nao esta em usuarios de teste | Adicionar o Gmail em `Publico-alvo > Usuarios de teste` |
| Botao Google continua em outro usuario | Cookie Google antigo | O sistema ja limpa `GoogleExternal` no login/logout |

## Fluxo OAuth2 M2M

M2M significa Machine-to-Machine: uma aplicacao externa acessa a API sem usuario humano logado.

Fluxo:

1. Cliente tecnico chama `/api/oauth/token`.
2. Envia `client_id` e `client_secret`.
3. A API emite um Bearer token.
4. Cliente chama `/api/m2m/export/{id}` com o token.
5. Documento e exportado para `Storage/external/m2m-storage`.

Credenciais M2M:

| Campo | Valor |
|---|---|
| `client_id` | `storage-client` |
| `client_secret` | `M2M@123` |
| `scope` | `exports.m2m` |

Comando de teste:

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

Tambem existe o script:

```powershell
.\docs\demo-m2m.ps1
```

## Como Demonstrar

1. Rodar a aplicacao.
2. Entrar como `admin`.
3. Mostrar `/api/auth/me` no Swagger com roles e permissoes.
4. Enviar documento `Confidencial`.
5. Entrar como `aluno` e mostrar que o documento `Confidencial` nao aparece.
6. Mostrar que aluno so consegue enviar `Publico`.
7. Entrar como `gestor` e mostrar documentos, sem auditoria e sem usuarios.
8. Entrar como `auditor` e mostrar apenas auditoria.
9. Conectar Google e exportar documento permitido para Drive.
10. Executar fluxo M2M.
11. Abrir auditoria e mostrar os eventos gerados.

## Como Provar que o Login e LDAP

No codigo:

- `AuthController` chama `IDirectoryService.AuthenticateAsync`.
- O `Program.cs` registra `IDirectoryService` como `LdapDirectoryService`.
- `LdapDirectoryService` usa `Novell.Directory.Ldap.NETStandard`.
- O SQLite nao possui tabela de usuarios nem senha.

Na demonstracao:

- login valido gera `ldap.login.success`;
- login invalido gera `ldap.login.failed`;
- alterar grupo no LDAP muda o papel no proximo login;
- no servidor EC2 e possivel observar conexoes na porta 389.

Comandos uteis no servidor EC2:

```bash
sudo tcpdump -ni any port 389
```

Ubuntu/Debian:

```bash
sudo tail -f /var/log/syslog
```

Amazon Linux/RHEL:

```bash
sudo tail -f /var/log/messages
```

## Endpoints Principais

| Controller | Rota | Funcao |
|---|---|---|
| `AuthController` | `/api/auth` | Login LDAP, logout e sessao atual |
| `DocumentsController` | `/api/documents` | Upload, listagem, download, exclusao e Drive |
| `GoogleController` | `/api/google` | Conectar/desconectar Google |
| `UsersController` | `/api/users` | Listar usuarios LDAP e alterar papel |
| `RbacController` | `/api/rbac` | Mostrar matriz RBAC |
| `AuditController` | `/api/audit` | Consultar logs |
| `OAuthController` | `/api/oauth/token` | Emitir token M2M |
| `M2MController` | `/api/m2m/export/{id}` | Exportar por token Bearer |

## Documentacao do Projeto

| Arquivo | Uso |
|---|---|
| `docs/relatorio-tecnico.md` | Relatorio formal da entrega |
| `docs/guia-do-codigo.md` | Explicacao do codigo por partes |
| `docs/diagrama-rbac.md` | Diagrama RBAC detalhado |
| `docs/diagrama-rbac.mmd` | Mermaid puro do diagrama RBAC |
| `docs/fluxograma-rbac.md` | Fluxo de decisao RBAC |
| `docs/roteiro-video.md` | Roteiro para demonstracao em video |
| `docs/roteiro-slides-canva.md` | Roteiro para criar slides no Canva AI |
| `docs/demo-m2m.ps1` | Script PowerShell do fluxo M2M |

## Frase Final para Apresentacao

O Portal IAM Docs demonstra IAM em uma aplicacao real: o LDAP autentica e fornece grupos, o RBAC transforma grupos em permissoes, os controllers protegem os recursos, o Google OIDC permite exportacao externa, o OAuth2 M2M permite integracao tecnica e a auditoria registra os eventos relevantes.
