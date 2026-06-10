# Roteiro do Video de Demonstracao

Duracao sugerida: 5 a 7 minutos.

Objetivo do video: mostrar a aplicacao funcionando e comprovar os requisitos da atividade:

- login LDAP;
- diferenca de permissoes por papel RBAC;
- exportacao Google usando OpenID Connect;
- exportacao M2M usando OAuth2;
- auditoria.

## Antes de gravar

Deixe a aplicacao rodando:

```powershell
dotnet run --urls http://localhost:5169
```

Abra no navegador:

```text
http://localhost:5169
http://localhost:5169/swagger
```

Tenha pelo menos um documento `Confidencial` enviado por `admin` ou `gestor` para demonstrar que o `aluno` nao consegue ver.

## 1. Abertura - 20 segundos

**Mostrar na tela:** pagina inicial do sistema.

**Fala sugerida:**

> Este e o Portal IAM Docs, um sistema de gestao de documentos feito em C# com ASP.NET Core Web API. O foco do projeto e demonstrar autenticacao centralizada por LDAP, autorizacao por RBAC, integracao com Google via OpenID Connect, exportacao M2M por OAuth2 e auditoria.

## 2. Arquitetura Rapida - 30 segundos

**Mostrar na tela:** estrutura de pastas no VS Code ou README.

**Fala sugerida:**

> O projeto esta dividido em Back e Front. O Back possui Controllers, DTOs, Models e Services. O Front usa HTML, CSS e Bootstrap, sem Razor. O SQLite guarda apenas documentos e auditoria. A identidade dos usuarios fica no LDAP real da EC2.

**Pontos para mostrar:**

- `Back/Controllers`
- `Back/Core/Services`
- `Front/wwwroot`
- `Storage/iam-documents.db`
- `Storage/Documents`

## 3. Login LDAP - 50 segundos

**Mostrar na tela:** login com `admin`.

Credenciais:

```text
admin / Admin@123
```

**Fala sugerida:**

> Agora vou fazer login como administrador. Esse login nao consulta o banco local. A aplicacao chama o servidor LDAP configurado na EC2. Se o bind LDAP for valido, o sistema cria um cookie de sessao e carrega os grupos do usuario como roles.

**Depois de logar:** abrir Swagger em:

```text
GET /api/auth/me
```

**Fala sugerida:**

> Aqui no Swagger e possivel ver o usuario logado, o papel Administrador e as permissoes RBAC carregadas na sessao.

## 4. RBAC com Administrador - 40 segundos

**Mostrar na tela:** painel do admin.

**Fala sugerida:**

> O administrador tem controle total. Ele consegue enviar documentos, visualizar todos os documentos, excluir, conectar Google Drive, acessar usuarios LDAP e consultar auditoria.

**Acao:**

Enviar um documento com classificacao:

```text
Confidencial
```

**Fala sugerida:**

> Este documento foi enviado como Confidencial. O arquivo fisico fica em Storage/Documents, e os metadados ficam no SQLite.

## 5. RBAC com Usuario/aluno - 50 segundos

**Mostrar na tela:** sair e logar como aluno.

Credenciais:

```text
aluno / Aluno@123
```

**Fala sugerida:**

> Agora vou entrar como aluno, que possui o papel Usuario. Pela regra RBAC atual, o Usuario comum so pode enviar, visualizar, baixar e exportar documentos classificados como Publico.

**Mostrar:**

- documento `Confidencial` nao aparece;
- seletor de upload mostra apenas `Publico`;
- auditoria e usuarios LDAP nao aparecem.

**Fala sugerida:**

> Mesmo que o aluno tente acessar documento Confidencial diretamente pela API, o back-end bloqueia. A regra nao esta apenas escondida no front, ela tambem esta validada nos controllers.

## 6. RBAC com Gestor - 35 segundos

**Mostrar na tela:** sair e logar como gestor.

Credenciais:

```text
gestor / Gestor@123
```

**Fala sugerida:**

> O gestor tem acesso operacional aos documentos. Ele consegue enviar documentos Publico, Interno ou Confidencial, visualizar todos e exportar para Google Drive. Mas ele nao acessa auditoria e nao altera papeis de usuarios.

**Mostrar:**

- documentos aparecem;
- upload permite todas as classificacoes;
- painel de auditoria nao aparece;
- painel de usuarios nao aparece.

## 7. RBAC com Auditor - 30 segundos

**Mostrar na tela:** sair e logar como auditor.

Credenciais:

```text
auditor / Auditor@123
```

**Fala sugerida:**

> O auditor e um papel separado para governanca. Ele nao envia nem visualiza documentos. Ele acessa apenas os logs de auditoria.

**Mostrar:**

- sem lista de documentos;
- sem upload;
- auditoria visivel.

## 8. Governanca LDAP - 40 segundos

**Mostrar na tela:** voltar como `admin`, abrir painel de usuarios.

**Fala sugerida:**

> Apenas o administrador pode alterar o papel de um usuario. Essa alteracao nao e feita no SQLite. O sistema atualiza os grupos no LDAP. O usuario precisa fazer novo login para receber o novo papel no cookie.

**Acao sugerida:**

Mostrar o formulario de troca de papel, mas nao precisa alterar se nao quiser mexer nos usuarios durante a gravacao.

## 9. Google OIDC e Google Drive - 50 segundos

**Mostrar na tela:** painel do admin ou gestor.

**Fala sugerida:**

> Agora vou demonstrar a integracao com Google. O login principal continua sendo LDAP. O Google entra depois, apenas para conectar uma conta externa e autorizar a exportacao para o Drive usando OpenID Connect.

**Acao:**

1. Clicar em `Conectar Google`.
2. Autorizar com uma conta de teste.
3. Voltar ao painel.
4. Clicar em `Drive` em um documento permitido.

**Fala sugerida:**

> A aplicacao usa o token retornado pelo Google e envia o arquivo pela Google Drive API. Esse fluxo atende ao requisito de integracao OIDC.

## 10. OAuth2 M2M - 1 minuto

**Mostrar na tela:** Swagger.

**Fala sugerida:**

> O fluxo M2M representa uma integracao entre sistemas, sem usuario humano logado. Um cliente tecnico usa client_id e client_secret para receber um token OAuth2. Depois usa esse token para exportar um documento.

**Passo 1:** abrir:

```text
07 - OAuth2 M2M Token
POST /api/oauth/token
```

Enviar:

```json
{
  "client_id": "storage-client",
  "client_secret": "M2M@123"
}
```

**Mostrar resposta:**

```json
{
  "access_token": "...",
  "token_type": "Bearer",
  "expires_in": 1800,
  "scope": "exports.m2m"
}
```

**Passo 2:** clicar em `Authorize` no Swagger e colar apenas o `access_token`.

**Passo 3:** abrir:

```text
08 - Exportacao M2M
POST /api/m2m/export/{id}
```

Executar com o ID de um documento.

**Fala sugerida:**

> A exportacao foi autorizada pelo token Bearer e o arquivo foi copiado para o storage externo simulado.

**Mostrar pasta:**

```text
Storage/external/m2m-storage
```

## 11. Auditoria - 40 segundos

**Mostrar na tela:** entrar como admin ou auditor e abrir auditoria.

**Fala sugerida:**

> Por fim, a aplicacao registra eventos importantes em auditoria no SQLite: login LDAP, falha de login, upload, download, troca de papel, exportacao Google, emissao de token M2M e exportacao M2M.

**Mostrar eventos:**

- `ldap.login.success`
- `document.upload`
- `google.drive.export.success`
- `oauth2.token.issued`
- `m2m.export.success`

## 12. Encerramento - 20 segundos

**Mostrar na tela:** README ou painel final.

**Fala sugerida:**

> Com isso, o projeto demonstra os pontos principais de IAM: autenticacao centralizada com LDAP, autorizacao por RBAC, governanca de acesso, integracao federada com Google, autorizacao Machine-to-Machine por OAuth2 e auditoria dos eventos relevantes.

## Ordem curta se o video precisar ser menor

Use esta ordem se precisar gravar em ate 4 minutos:

1. Login admin via LDAP.
2. Mostrar `/api/auth/me`.
3. Enviar documento Confidencial.
4. Logar como aluno e mostrar que nao ve Confidencial.
5. Logar como auditor e mostrar apenas auditoria.
6. Conectar Google e exportar documento.
7. Gerar token M2M no Swagger e exportar.
8. Mostrar auditoria.

## Frases prontas

**LDAP**

> O usuario e a senha sao validados no servidor LDAP real da EC2. O SQLite nao guarda senha.

**RBAC**

> O grupo LDAP vira papel, e o papel define as permissoes. Os controllers validam essas permissoes antes de executar a acao.

**Documento Publico**

> O aluno so pode interagir com documentos Publico. Documentos Interno e Confidencial ficam restritos a administrador e gestor.

**Google OIDC**

> O Google nao substitui o login LDAP. Ele e usado para conectar uma conta externa e autorizar exportacao para Drive.

**M2M**

> O M2M mostra uma aplicacao externa usando client_id e client_secret para obter um Bearer token e exportar um documento sem login humano.

**Auditoria**

> A auditoria registra os eventos importantes no SQLite para demonstrar governanca e rastreabilidade.
