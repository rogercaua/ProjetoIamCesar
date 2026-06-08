# Relatorio Tecnico - Portal IAM Docs

## Identificacao

Projeto: Sistema de Gestao de Documentos com IAM.

Integrantes: preencher com os nomes do grupo antes da entrega.

Linguagem e plataforma: C#, ASP.NET Core Razor Pages, .NET 8.

## Objetivo

O projeto implementa um portal simples de upload e download de documentos com controles de identidade e acesso. O foco e demonstrar autenticacao centralizada, autorizacao por RBAC, exportacao OIDC e autorizacao M2M com OAuth2.

## Arquitetura

A aplicacao foi organizada em camadas simples:

- `Back/Controllers/`: rotas de API documentadas no Swagger.
- `Back/Core/Dtos/`: objetos de entrada e saida usados pelos controllers.
- `Back/Core/Services/`: interfaces e implementacoes de autenticacao, RBAC, documentos, auditoria e exportacoes.
- `Back/Core/Models/`: entidades de usuario, documento, auditoria e definicao RBAC.
- `Front/Pages/`: telas Razor Pages para login, documentos, usuarios, auditoria e exportacao OIDC.

O armazenamento e local para fins didaticos. Documentos e trilhas de auditoria sao criados na pasta `Storage/` durante a execucao.

## Autenticacao centralizada LDAP

A autenticacao esta em `Back/Core/Services/DemoLdapDirectoryService.cs`. Para manter o projeto executavel sem servidor externo, o diretorio LDAP foi representado por `Data/demo-ldap-users.json`.

Fluxo logico:

1. O usuario informa login e senha em `Front/Pages/Account/Login`.
2. A aplicacao consulta o diretorio demo por `IDirectoryService.AuthenticateAsync`.
3. Se as credenciais forem validas, a aplicacao cria uma sessao por cookie.
4. Os papeis vindos do diretorio sao colocados como claims de role.
5. A auditoria registra sucesso ou falha de login.

Em um ambiente real, a classe de diretorio pode ser substituida por bind LDAP contra Active Directory, OpenLDAP ou outro diretorio corporativo.

## Modelagem RBAC

A matriz RBAC esta em `Back/Core/Services/RbacService.cs`. Os papeis sao:

| Papel | Finalidade |
|---|---|
| Administrador | Controle total, inclusive governanca de usuarios |
| Gestor | Gerencia documentos e acompanha auditoria |
| Usuario | Opera apenas os proprios documentos |
| Auditor | Consulta documentos e eventos sem alterar dados |
| ServicoM2M | Conta tecnica para exportacao por API |

Permissoes principais:

- `documents.upload`
- `documents.view.own`
- `documents.view.all`
- `documents.download.own`
- `documents.download.all`
- `documents.delete`
- `exports.oidc`
- `exports.m2m`
- `users.manage.roles`
- `audit.view`

A verificacao e feita antes de cada acao sensivel. Por exemplo, o usuario comum so consegue ver e baixar documentos em que `OwnerUserName` seja igual ao nome da sessao.

## Governanca de acesso

A tela `Front/Pages/Admin/Users` permite ao administrador alterar o papel de cada usuario no diretorio demo. A mudanca fica registrada em `Data/demo-ldap-users.json` e passa a valer no proximo login do usuario.

Esse fluxo demonstra o conceito de governanca: identidade e papel ficam centralizados, enquanto a aplicacao consome esses atributos para decidir acesso.

## Integracao OIDC

A exportacao OIDC esta em `Front/Pages/Exports/Oidc` e `Back/Core/Services/OidcExportService.cs`.

Fluxo logico:

1. Usuario autenticado escolhe um documento.
2. A aplicacao verifica `exports.oidc` e permissao de visualizar o documento.
3. A tela apresenta uma conta Google demo e pede consentimento.
4. O servico copia o arquivo para `Storage/external/oidc-google-drive`.
5. Um artefato JSON e gerado com protocolo, provedor e claims simuladas de ID token.
6. A auditoria registra a exportacao.

O objetivo e demonstrar o papel do OIDC: identificar o usuario externo e associar a exportacao a uma identidade autenticada pelo provedor.

## OAuth2 M2M

A API M2M esta em `Back/Controllers/OAuthController.cs`, `Back/Controllers/M2MController.cs`, `Back/Core/Services/M2MTokenService.cs` e `Back/Core/Services/M2MStorageExportService.cs`.

Fluxo logico:

1. Cliente tecnico chama `POST /api/oauth/token` com `client_id` e `client_secret`.
2. A aplicacao valida as credenciais configuradas em `appsettings.json`.
3. Um token Bearer com escopo `exports.m2m` e emitido por 30 minutos.
4. O cliente chama `POST /api/m2m/export/{id}` com o token.
5. O documento e copiado para `Storage/external/m2m-storage`.
6. A auditoria registra emissao do token e exportacao.

Credenciais demo:

- `client_id`: `storage-client`
- `client_secret`: `M2M@123`

## Auditoria

O servico `AuditService` grava eventos em `Storage/audit.log`. A tela `Front/Pages/Audit/Index` mostra eventos recentes para papeis com `audit.view`.

Eventos registrados:

- login LDAP com sucesso ou falha
- logout
- upload
- download
- exclusao
- troca de papel
- emissao de token M2M
- exportacao OIDC
- exportacao M2M

## Bibliotecas utilizadas

- ASP.NET Core Razor Pages: telas web e rotas.
- Microsoft.AspNetCore.Authentication.Cookies: sessao autenticada por cookie.
- System.Text.Json: leitura e gravacao de dados locais.
- System.Security.Cryptography: geracao de token aleatorio para M2M.

Nao foram adicionados pacotes NuGet externos para manter o projeto simples.

## Como demonstrar

1. Rodar a aplicacao com `dotnet run --urls http://localhost:5169`.
2. Entrar como `admin` e enviar um documento.
3. Entrar como `aluno` e mostrar que ele so ve os proprios documentos.
4. Voltar como `admin`, alterar o papel do aluno para `Gestor`.
5. Fazer novo login como `aluno` e mostrar que agora ele ve todos os documentos.
6. Exportar um documento via OIDC.
7. Executar o fluxo M2M pelo PowerShell com token OAuth2.
8. Abrir a auditoria e mostrar os eventos gerados.
