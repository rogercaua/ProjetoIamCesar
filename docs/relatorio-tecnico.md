# Relatorio Tecnico - Portal IAM Docs

## 1. Identificacao

Projeto: Sistema de Gestao de Documentos com IAM.

Integrantes: preencher com os nomes do grupo antes da entrega.

Linguagem e plataforma: C#, ASP.NET Core Web API, .NET 8, HTML, CSS, Bootstrap, EF Core, SQLite, LDAP, OpenID Connect e OAuth2.

## 2. Objetivo

O projeto implementa um portal de documentos com controle de identidade e acesso. A aplicacao permite login por LDAP, upload e download de arquivos, exportacao para Google Drive, exportacao tecnica por OAuth2 M2M, controle de permissoes por RBAC e auditoria das acoes principais.

O foco do projeto nao e uma regra de negocio complexa. O foco e mostrar como autenticar, autorizar e auditar usuarios de forma centralizada.

## 3. Arquitetura

A aplicacao foi separada em back-end, core e front:

| Camada | Pasta | Responsabilidade |
|---|---|---|
| Controllers/API | `Back/Controllers` | Receber requisicoes HTTP, validar permissao e chamar servicos |
| Core/Data | `Back/Core/Data` | Configurar EF Core e SQLite |
| Core/DTOs | `Back/Core/Dtos` | Padronizar entradas e respostas da API |
| Core/Models | `Back/Core/Models` | Representar entidades e constantes do dominio |
| Core/Services | `Back/Core/Services` | Implementar regras de LDAP, RBAC, documentos, auditoria e exportacoes |
| Front | `Front/wwwroot` | Login e estilos em HTML/CSS/Bootstrap, sem Razor |
| Storage | `Storage` | Banco SQLite, arquivos enviados e exportacoes |

O front de login e estatico. O painel `/dashboard` e renderizado pelo `PortalController` como HTML puro para mostrar dados reais do banco e do LDAP sem usar Razor e sem JavaScript.

## 4. Banco de dados e armazenamento

O projeto usa SQLite com EF Core.

O banco fica em:

```text
Storage/iam-documents.db
```

O SQLite guarda:

- metadados dos documentos
- nome original do arquivo
- dono do documento
- tamanho
- tipo MIME
- data de upload
- nivel de classificacao
- logs de auditoria

O SQLite nao guarda senha e nao autentica usuario.

Os arquivos fisicos enviados ficam em:

```text
Storage/Documents
```

## 5. Autenticacao centralizada por LDAP

A autenticacao esta em `Back/Core/Services/LdapDirectoryService.cs`, usando a biblioteca `Novell.Directory.Ldap.NETStandard`.

Fluxo de login:

1. O usuario informa usuario e senha na tela `Front/wwwroot/index.html`.
2. A tela envia `POST /api/auth/login`.
3. `AuthController` recebe a requisicao.
4. `AuthController` chama `IDirectoryService.AuthenticateAsync`.
5. A implementacao real e `LdapDirectoryService`.
6. O servico abre conexao com o LDAP configurado em `appsettings.json`.
7. O servico faz bind administrativo para buscar o DN do usuario.
8. O servico tenta bind com o DN do usuario e a senha digitada.
9. Se o bind der certo, o usuario esta autenticado.
10. O servico busca os grupos LDAP do usuario.
11. Os grupos LDAP viram roles da aplicacao.
12. O ASP.NET Core cria um cookie de sessao.
13. A auditoria grava `ldap.login.success`.

Se o usuario ou senha estiver incorreto, a auditoria grava `ldap.login.failed`.

## 6. Mapeamento LDAP para roles

| Grupo no LDAP | Role na aplicacao |
|---|---|
| `cn=Administradores,ou=groups,dc=projetoiam,dc=local` | `Administrador` |
| `cn=Gestores,ou=groups,dc=projetoiam,dc=local` | `Gestor` |
| `cn=Usuarios,ou=groups,dc=projetoiam,dc=local` | `Usuario` |
| `cn=Auditores,ou=groups,dc=projetoiam,dc=local` | `Auditor` |

Usuarios usados na demonstracao:

| Usuario | Senha | Papel |
|---|---|---|
| `admin` | `Admin@123` | Administrador |
| `gestor` | `Gestor@123` | Gestor |
| `aluno` | `Aluno@123` | Usuario |
| `auditor` | `Auditor@123` | Auditor |

## 7. Modelagem RBAC

A matriz RBAC esta em:

```text
Back/Core/Services/RbacService.cs
Back/Core/Models/RbacDefinition.cs
```

Roles:

| Papel | Finalidade |
|---|---|
| Administrador | Controle total, inclusive documentos, usuarios, auditoria e exportacoes |
| Gestor | Gerencia documentos e exportacoes |
| Usuario | Envia e acessa apenas os proprios documentos |
| Auditor | Consulta apenas eventos de auditoria |
| ServicoM2M | Conta tecnica usada para exportacao automatizada |

Permissoes:

| Permissao | Significado |
|---|---|
| `documents.upload` | Pode enviar documento |
| `documents.view.own` | Pode listar documentos proprios |
| `documents.view.all` | Pode listar documentos de todos |
| `documents.download.own` | Pode baixar documentos proprios |
| `documents.download.all` | Pode baixar documentos de todos |
| `documents.delete` | Pode excluir documentos |
| `exports.google_drive` | Pode exportar para Google Drive |
| `exports.m2m` | Pode exportar via M2M |
| `users.manage.roles` | Pode alterar papel no LDAP |
| `audit.view` | Pode consultar auditoria |

Matriz resumida:

| Papel | Permissoes principais |
|---|---|
| Administrador | Todas as permissoes |
| Gestor | Upload, ver todos, baixar todos, exportar Google Drive |
| Usuario | Upload, ver proprios, baixar proprios, exportar Google Drive |
| Auditor | Ver auditoria |
| ServicoM2M | Exportar M2M |

## 8. Como a autorizacao e aplicada

As permissoes nao ficam apenas na tela. Elas sao verificadas no back-end.

Exemplos:

- `DocumentsController.GetAll` so lista documentos se o usuario tiver `documents.view.all` ou `documents.view.own`.
- `DocumentsController.Download` usa `CanDownloadDocument` para impedir acesso indevido.
- `DocumentsController.Delete` exige `documents.delete`, portanto apenas administrador.
- `UsersController.UpdateRole` exige `users.manage.roles`, portanto apenas administrador.
- `AuditController.GetRecent` exige `audit.view`, portanto administrador ou auditor.
- `M2MController.Export` exige token Bearer valido com escopo `exports.m2m`.

Se o usuario tentar acessar uma rota sem permissao, a API retorna `403 Forbidden`.

## 9. Telas por papel

| Papel | Tela liberada |
|---|---|
| Administrador | Documentos, envio, Drive, usuarios LDAP e auditoria |
| Gestor | Documentos, envio e Drive |
| Usuario | Envio, Drive e apenas os proprios documentos |
| Auditor | Apenas auditoria |

Isso e feito no `PortalController`, que verifica as permissoes antes de montar cada bloco visual.

## 10. Governanca de acesso

A troca de papel e feita no LDAP, nao no banco local.

Fluxo:

1. O administrador acessa o painel.
2. O painel consulta os usuarios LDAP.
3. O administrador seleciona usuario e novo papel.
4. A tela envia `POST /api/users/role`.
5. `UsersController` valida `users.manage.roles`.
6. `LdapDirectoryService.UpdateRoleAsync` remove o usuario dos grupos antigos.
7. O mesmo metodo adiciona o usuario ao grupo LDAP do novo papel.
8. A auditoria grava `directory.role.changed`.
9. O usuario precisa fazer novo login para receber a nova role no cookie.

## 11. OpenID Connect e Google Drive

O Google nao e usado para login principal da aplicacao. O login principal e LDAP.

O Google e usado para conectar uma conta externa e permitir exportacao para Google Drive.

Fluxo:

1. Usuario autenticado por LDAP clica em `Conectar Google`.
2. `GoogleController.Connect` inicia o desafio OIDC.
3. O Google autentica a conta e retorna para `/signin-google`.
4. A aplicacao salva os tokens em um cookie separado chamado `DocumentPortalIam.Google`.
5. O usuario clica em `Drive` em um documento permitido.
6. `GoogleDriveExportService` pega o access token salvo.
7. A Google Drive API recebe o arquivo.
8. A auditoria grava `google.drive.export.success`.

Escopos configurados:

```text
openid
profile
email
https://www.googleapis.com/auth/drive.file
```

## 12. OAuth2 M2M

O fluxo M2M simula uma integracao tecnica, sem usuario humano.

Fluxo:

1. Cliente tecnico chama `POST /api/oauth/token`.
2. Envia `client_id` e `client_secret`.
3. `OAuthController` chama `M2MTokenService`.
4. Se as credenciais estiverem corretas, a API emite um Bearer token.
5. O cliente chama `POST /api/m2m/export/{id}`.
6. `M2MController` valida o token e o escopo.
7. `M2MStorageExportService` copia o arquivo para `Storage/external/m2m-storage`.
8. A auditoria registra a exportacao.

Credenciais usadas:

| Campo | Valor |
|---|---|
| `client_id` | `storage-client` |
| `client_secret` | `M2M@123` |
| `scope` | `exports.m2m` |

## 13. Auditoria

A auditoria fica em `AuditService`.

Eventos gravados:

- login LDAP com sucesso
- falha de login LDAP
- logout
- upload de documento
- download de documento
- exclusao de documento
- troca de papel no LDAP
- emissao de token M2M
- exportacao para Google Drive
- exportacao M2M

O endpoint de consulta e:

```text
GET /api/audit
```

Somente `Administrador` e `Auditor` possuem `audit.view`.

## 14. Swagger

O Swagger fica em:

```text
http://localhost:5169/swagger
```

Os controllers usam `SwaggerOperation` para explicar quem pode acessar cada rota e o que ela faz.

## 15. Bibliotecas utilizadas

| Biblioteca | Uso |
|---|---|
| `Microsoft.AspNetCore.Authentication.Cookies` | Criar sessao local por cookie |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | Conectar conta Google via OIDC |
| `Novell.Directory.Ldap.NETStandard` | Autenticacao e consulta LDAP |
| `Microsoft.EntityFrameworkCore.Sqlite` | Banco SQLite |
| `Google.Apis.Drive.v3` | Envio de arquivo para Google Drive |
| `Swashbuckle.AspNetCore` | Swagger |
| `System.Security.Cryptography` | Geracao de tokens M2M |

## 16. Como demonstrar

1. Rodar `dotnet run --urls http://localhost:5169`.
2. Entrar como `admin`.
3. Mostrar `/api/auth/me` no Swagger para provar role e permissoes.
4. Enviar documento.
5. Mostrar que o documento aparece no SQLite e o arquivo fisico em `Storage/Documents`.
6. Entrar como `aluno` e mostrar que ele so ve os proprios documentos.
7. Entrar como `auditor` e mostrar que ele nao ve documentos, apenas auditoria.
8. Entrar como `gestor` e mostrar que ele ve documentos, mas nao ve logs.
9. Conectar Google por OIDC.
10. Exportar documento para Google Drive.
11. Executar `docs/demo-m2m.ps1`.
12. Abrir a auditoria e mostrar os eventos.

## 17. Resposta curta para perguntas provaveis

**O login usa banco de dados?**

Nao. O login usa LDAP. O SQLite guarda documentos e logs.

**Por que tem cookie?**

Depois que o LDAP valida usuario e senha, a aplicacao cria um cookie para manter a sessao local.

**Por que precisa fazer novo login depois de trocar papel?**

Porque as roles ficam nas claims do cookie. Ao logar de novo, o sistema consulta o LDAP e cria um cookie novo.

**O Google e usado para autenticar no sistema?**

Nao. O Google e usado para conectar uma conta externa e exportar para Drive. A autenticacao principal continua sendo LDAP.

**O auditor pode ver documentos?**

Nao. O auditor so possui `audit.view`.
