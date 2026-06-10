# Relatorio Tecnico - Portal IAM Docs

## 1. Identificacao

**Projeto:** Portal IAM Docs - Sistema de Gestao de Documentos com IAM.

**Disciplina:** Gestao de Identidade e Acesso.

**Integrantes:** preencher com os nomes dos componentes do grupo antes da entrega.

**Linguagem e plataforma:** C#, ASP.NET Core Web API, .NET 8, HTML, CSS, Bootstrap, SQLite, LDAP, OpenID Connect, Google Drive API e OAuth2.

## 2. Objetivo

O projeto implementa um portal de documentos com autenticacao, autorizacao e auditoria. A aplicacao permite que usuarios facam login usando um servidor LDAP real, acessem documentos conforme o papel RBAC, conectem uma conta Google por OpenID Connect, exportem documentos para o Google Drive e executem uma exportacao tecnica por OAuth2 Machine-to-Machine.

O foco do projeto e demonstrar os conceitos de IAM em uma aplicacao funcional:

- autenticacao centralizada;
- autorizacao baseada em papeis;
- governanca de acesso;
- integracao federada;
- autorizacao entre sistemas;
- rastreabilidade por auditoria.

## 3. Visao Geral da Solucao

A regra de negocio escolhida foi um Sistema de Gestao de Documentos. A aplicacao permite upload, listagem, download, exclusao e exportacao de arquivos, com acesso controlado por perfil.

Principais componentes:

| Componente | Uso no projeto |
|---|---|
| Front-end | HTML, CSS e Bootstrap em `Front/wwwroot` |
| API | Controllers ASP.NET Core em `Back/Controllers` |
| Core | Servicos, interfaces, modelos e DTOs em `Back/Core` |
| LDAP | Autenticacao e grupos dos usuarios |
| SQLite | Metadados de documentos e logs de auditoria |
| Storage local | Arquivos fisicos enviados |
| Google OIDC | Conexao da conta Google |
| Google Drive API | Exportacao de documentos para Drive |
| OAuth2 M2M | Exportacao tecnica sem usuario humano |
| Swagger | Teste e documentacao dos endpoints |

## 4. Organizacao do Codigo

```text
Back/
  Controllers/
    AuthController.cs
    DocumentsController.cs
    GoogleController.cs
    UsersController.cs
    RbacController.cs
    AuditController.cs
    OAuthController.cs
    M2MController.cs
    PortalController.cs

  Core/
    Data/
      AppDbContext.cs
    Dtos/
      AuthDtos.cs
      DocumentDtos.cs
      AuditDtos.cs
      RbacDtos.cs
      CommonDtos.cs
      DtoMappings.cs
    Models/
      AppUser.cs
      DocumentRecord.cs
      AuditRecord.cs
      RbacDefinition.cs
    Services/
      LdapDirectoryService.cs
      RbacService.cs
      DocumentRepository.cs
      AuditService.cs
      GoogleDriveExportService.cs
      M2MTokenService.cs
      M2MStorageExportService.cs
      interfaces correspondentes

Front/
  wwwroot/
    index.html
    dashboard.html
    css/site.css
    lib/bootstrap/

Storage/
  iam-documents.db
  Documents/
  external/m2m-storage/

docs/
  diagrama-rbac.md
  fluxograma-rbac.md
  guia-do-codigo.md
  roteiro-video.md
  roteiro-slides-canva.md
```

## 5. Banco de Dados e Armazenamento

O projeto utiliza SQLite com Entity Framework Core. O banco fica em:

```text
Storage/iam-documents.db
```

O SQLite armazena:

- metadados dos documentos;
- nome original do arquivo;
- nome fisico salvo;
- dono do documento;
- classificacao;
- tamanho;
- tipo de conteudo;
- data de upload;
- logs de auditoria.

O SQLite nao guarda usuario nem senha. A autenticacao e realizada pelo LDAP.

Os arquivos enviados ficam fisicamente em:

```text
Storage/Documents
```

As exportacoes M2M ficam em:

```text
Storage/external/m2m-storage
```

## 6. Requisito 1 - Modelagem RBAC

O RBAC foi implementado em:

```text
Back/Core/Models/RbacDefinition.cs
Back/Core/Services/RbacService.cs
```

### 6.1 Papeis

| Papel | Origem | Finalidade |
|---|---|---|
| Administrador | Grupo LDAP `Administradores` | Controle total do sistema |
| Gestor | Grupo LDAP `Gestores` | Operacao de documentos e exportacao Google |
| Usuario | Grupo LDAP `Usuarios` | Acesso apenas a documentos Publico |
| Auditor | Grupo LDAP `Auditores` | Consulta de auditoria |
| ServicoM2M | Credencial tecnica | Exportacao entre sistemas |

### 6.2 Permissoes

| Permissao | Significado |
|---|---|
| `documents.upload` | Pode enviar documentos |
| `documents.view.public` | Pode visualizar documentos Publico |
| `documents.view.all` | Pode visualizar todos os documentos |
| `documents.download.public` | Pode baixar documentos Publico |
| `documents.download.all` | Pode baixar todos os documentos |
| `documents.delete` | Pode excluir documentos |
| `exports.google_drive` | Pode exportar documentos permitidos para Google Drive |
| `exports.m2m` | Pode exportar documentos por fluxo M2M |
| `users.manage.roles` | Pode alterar papeis no LDAP |
| `audit.view` | Pode consultar logs de auditoria |

### 6.3 Matriz RBAC

| Acao | Administrador | Gestor | Usuario | Auditor | ServicoM2M |
|---|---:|---:|---:|---:|---:|
| Enviar documento Publico | Sim | Sim | Sim | Nao | Nao |
| Enviar documento Interno | Sim | Sim | Nao | Nao | Nao |
| Enviar documento Confidencial | Sim | Sim | Nao | Nao | Nao |
| Ver documentos Publico | Sim | Sim | Sim | Nao | Nao |
| Ver documentos Interno/Confidencial | Sim | Sim | Nao | Nao | Nao |
| Baixar documentos Publico | Sim | Sim | Sim | Nao | Nao |
| Baixar documentos Interno/Confidencial | Sim | Sim | Nao | Nao | Nao |
| Excluir documento | Sim | Nao | Nao | Nao | Nao |
| Exportar para Google Drive | Sim | Sim | Apenas Publico | Nao | Nao |
| Alterar papeis LDAP | Sim | Nao | Nao | Nao | Nao |
| Consultar auditoria | Sim | Nao | Nao | Sim | Nao |
| Exportar via M2M | Sim | Nao | Nao | Nao | Sim |

### 6.4 Aplicacao do RBAC no Back-end

As permissoes sao verificadas no back-end, nao apenas escondidas no front.

Exemplos:

- `DocumentsController.GetAll` lista apenas documentos permitidos pelo papel.
- `DocumentsController.Upload` bloqueia `Usuario` tentando enviar `Interno` ou `Confidencial`.
- `DocumentsController.Download` impede download de documento restrito.
- `DocumentsController.ExportGoogleDriveCore` valida se o usuario pode ver o documento antes de exportar.
- `UsersController.UpdateRole` exige `users.manage.roles`.
- `AuditController.GetRecent` exige `audit.view`.

## 7. Requisito 2 - Autenticacao Centralizada LDAP

A autenticacao centralizada foi implementada em:

```text
Back/Core/Services/LdapDirectoryService.cs
Back/Controllers/AuthController.cs
```

Biblioteca usada:

```text
Novell.Directory.Ldap.NETStandard
```

Configuracao LDAP em `appsettings.json`:

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

### 7.1 Usuarios LDAP usados

| Usuario | Senha | Grupo LDAP | Papel no sistema |
|---|---|---|---|
| `admin` | `Admin@123` | `Administradores` | Administrador |
| `gestor` | `Gestor@123` | `Gestores` | Gestor |
| `aluno` | `Aluno@123` | `Usuarios` | Usuario |
| `auditor` | `Auditor@123` | `Auditores` | Auditor |

### 7.2 Fluxo de Login

1. Usuario informa login e senha na tela `index.html`.
2. A tela envia `POST /api/auth/login`.
3. `AuthController` chama `IDirectoryService.AuthenticateAsync`.
4. A implementacao usada e `LdapDirectoryService`.
5. O servico faz bind administrativo no LDAP.
6. O servico busca o usuario pelo filtro `(uid={0})`.
7. O sistema tenta bind com o DN do usuario e a senha digitada.
8. Se o bind for valido, o usuario esta autenticado.
9. O servico consulta os grupos LDAP do usuario.
10. Os grupos viram roles RBAC.
11. O ASP.NET Core cria o cookie de sessao.
12. A auditoria registra `ldap.login.success`.

Em caso de falha, a auditoria registra `ldap.login.failed`.

## 8. Requisito 3 - Integracao OIDC e Google Drive

A integracao com Google foi implementada em:

```text
Program.cs
Back/Controllers/GoogleController.cs
Back/Core/Services/GoogleDriveExportService.cs
```

Bibliotecas usadas:

```text
Microsoft.AspNetCore.Authentication.OpenIdConnect
Google.Apis.Drive.v3
```

### 8.1 Papel do OIDC

O Google nao substitui o login principal. O login principal continua sendo LDAP.

O OpenID Connect e usado depois do login LDAP para conectar uma conta Google externa. Essa conta fornece token para exportacao de documentos para o Google Drive.

### 8.2 Escopos configurados

```text
openid
profile
email
https://www.googleapis.com/auth/drive.file
```

### 8.3 Fluxo OIDC + Drive

1. Usuario faz login no sistema via LDAP.
2. Usuario clica em `Conectar Google`.
3. `GoogleController.Connect` inicia o fluxo OIDC.
4. Google autentica a conta externa.
5. Google retorna para `/signin-google`.
6. A aplicacao salva os tokens em um cookie separado chamado `DocumentPortalIam.Google`.
7. Usuario clica em `Drive` em um documento permitido.
8. `GoogleDriveExportService` recupera o access token.
9. A Google Drive API envia o arquivo para o Drive da conta conectada.
10. A auditoria registra `google.drive.export.success`.

### 8.4 Controle RBAC na exportacao Google

O endpoint de exportacao valida:

- se o usuario tem `exports.google_drive`;
- se o usuario pode visualizar o documento solicitado.

Com isso:

- Administrador exporta qualquer documento;
- Gestor exporta qualquer documento;
- Usuario exporta apenas documentos Publico;
- Auditor nao exporta documentos.

## 9. Requisito 4 - OAuth2 Machine-to-Machine

O fluxo M2M foi implementado em:

```text
Back/Controllers/OAuthController.cs
Back/Controllers/M2MController.cs
Back/Core/Services/M2MTokenService.cs
Back/Core/Services/M2MStorageExportService.cs
```

### 9.1 O que e M2M neste projeto

M2M significa Machine-to-Machine. No projeto, ele representa um sistema externo acessando a API sem usuario humano logado.

Em vez de usar login LDAP, o cliente tecnico usa:

```text
client_id = storage-client
client_secret = M2M@123
```

Se as credenciais estiverem corretas, a API gera um Bearer token com escopo:

```text
exports.m2m
```

### 9.2 Fluxo M2M

1. Cliente tecnico chama `POST /api/oauth/token`.
2. Envia `client_id` e `client_secret`.
3. `OAuthController` chama `M2MTokenService`.
4. `M2MTokenService` valida as credenciais.
5. A API retorna `access_token`, `token_type`, `expires_in` e `scope`.
6. Cliente chama `POST /api/m2m/export/{id}`.
7. Cliente envia `Authorization: Bearer {token}`.
8. `M2MController` valida o token e o escopo `exports.m2m`.
9. `M2MStorageExportService` copia o documento para `Storage/external/m2m-storage`.
10. A auditoria registra `m2m.export.success`.

### 9.3 Como demonstrar

No Swagger:

1. Executar `POST /api/oauth/token`.
2. Copiar o `access_token`.
3. Clicar em `Authorize`.
4. Colar apenas o token.
5. Executar `POST /api/m2m/export/{id}`.
6. Mostrar o arquivo em `Storage/external/m2m-storage`.

## 10. Auditoria

A auditoria foi implementada em:

```text
Back/Core/Services/AuditService.cs
Back/Controllers/AuditController.cs
```

Os logs ficam na tabela `AuditLogs` do SQLite.

Eventos registrados:

- `ldap.login.success`
- `ldap.login.failed`
- `session.logout`
- `document.upload`
- `document.download`
- `document.delete`
- `directory.role.changed`
- `google.drive.export.success`
- `oauth2.token.issued`
- `oauth2.token.denied`
- `m2m.export.success`
- `m2m.export.denied`

Somente `Administrador` e `Auditor` possuem permissao `audit.view`.

## 11. Governanca de Acesso

A governanca foi implementada na troca de papeis LDAP:

```text
Back/Controllers/UsersController.cs
Back/Core/Services/LdapDirectoryService.cs
```

Fluxo:

1. Administrador acessa o painel.
2. O painel lista usuarios consultando o LDAP.
3. Administrador escolhe usuario e novo papel.
4. A API valida a permissao `users.manage.roles`.
5. O sistema remove o usuario dos grupos antigos.
6. O sistema adiciona o usuario ao grupo LDAP do novo papel.
7. A auditoria registra `directory.role.changed`.
8. O usuario precisa fazer novo login para receber o novo papel no cookie.

Gestor, Usuario e Auditor nao alteram papeis.

## 12. Front-end

O front fica em:

```text
Front/wwwroot
```

Tecnologias:

- HTML;
- CSS;
- Bootstrap;
- sem Razor;
- sem JavaScript obrigatorio.

O login e uma pagina estatica. O painel `/dashboard` e renderizado pelo `PortalController` como HTML puro, permitindo exibir dados reais do SQLite e LDAP sem usar Razor.

O front tambem respeita o RBAC:

- Auditor nao ve upload nem documentos;
- Gestor nao ve auditoria nem usuarios;
- Usuario ve apenas upload Publico e documentos Publico;
- Administrador ve todos os blocos.

## 13. Swagger

O Swagger esta disponivel em:

```text
http://localhost:5169/swagger
```

Melhorias implementadas:

- grupos por area funcional;
- descricao geral com passo de teste;
- request body para login LDAP;
- request body para token M2M;
- botao `Authorize` para Bearer token M2M;
- respostas documentadas para `400`, `401`, `403` e `404`.

## 14. Bibliotecas Utilizadas

| Biblioteca | Finalidade |
|---|---|
| `Microsoft.AspNetCore.Authentication.Cookies` | Sessao do usuario por cookie |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | Conexao Google OIDC |
| `Novell.Directory.Ldap.NETStandard` | Autenticacao e consulta LDAP |
| `Microsoft.EntityFrameworkCore.Sqlite` | Banco SQLite |
| `Google.Apis.Drive.v3` | Exportacao para Google Drive |
| `Swashbuckle.AspNetCore` | Swagger UI |
| `Swashbuckle.AspNetCore.Annotations` | Descricao dos endpoints |
| `System.Security.Cryptography` | Geracao de token M2M |

## 15. Como Executar

```powershell
dotnet restore
dotnet run --urls http://localhost:5169
```

URLs principais:

```text
http://localhost:5169
http://localhost:5169/dashboard
http://localhost:5169/swagger
```

## 16. Roteiro Resumido de Demonstracao

1. Rodar a aplicacao.
2. Logar como `admin / Admin@123`.
3. Abrir `/api/auth/me` no Swagger.
4. Enviar documento `Confidencial`.
5. Logar como `aluno / Aluno@123` e mostrar que o Confidencial nao aparece.
6. Mostrar que aluno so envia `Publico`.
7. Logar como `gestor / Gestor@123` e mostrar acesso a documentos, mas sem auditoria.
8. Logar como `auditor / Auditor@123` e mostrar apenas auditoria.
9. Voltar como admin e mostrar a troca de papel LDAP.
10. Conectar Google por OIDC.
11. Exportar documento para Google Drive.
12. Gerar token M2M no Swagger.
13. Exportar documento via M2M.
14. Mostrar arquivo em `Storage/external/m2m-storage`.
15. Abrir auditoria e mostrar os eventos.

## 17. Evidencias no Codigo

| Requisito | Evidencia |
|---|---|
| LDAP | `LdapDirectoryService.AuthenticateAsync` faz bind LDAP |
| RBAC | `RbacService` mapeia roles para permissoes |
| Cookie Authentication | `Program.cs` configura `DocumentPortalIam.Auth` |
| OIDC | `Program.cs` configura `AddOpenIdConnect("Google")` |
| Drive | `GoogleDriveExportService` usa `DriveService` |
| OAuth2 M2M | `OAuthController` emite token e `M2MController` valida Bearer |
| SQLite | `AppDbContext` define `Documents` e `AuditLogs` |
| Auditoria | `AuditService.WriteAsync` grava eventos |
| Governanca | `UsersController` e `LdapDirectoryService.UpdateRoleAsync` alteram grupos LDAP |

## 18. Consideracoes Finais

O Portal IAM Docs atende aos requisitos principais da atividade. A aplicacao usa LDAP real para autenticacao centralizada, RBAC para autorizacao, SQLite para documentos e auditoria, OpenID Connect para conexao Google, Google Drive API para exportacao externa e OAuth2 M2M para integracao tecnica entre sistemas.

O projeto tambem demonstra governanca, pois o administrador consegue alterar papeis no LDAP, e as acoes sensiveis ficam registradas em auditoria.

## 19. Observacao sobre Kerberos

O bonus de Kerberos nao foi implementado. O projeto concentra a entrega obrigatoria em LDAP, RBAC, OIDC, OAuth2 M2M, auditoria e documentacao.
