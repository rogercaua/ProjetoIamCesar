# Roteiro para Slides - Portal IAM Docs

Use este texto no Canva AI para gerar a apresentacao. A ideia e criar uma apresentacao objetiva, tecnica e visual, mostrando que o projeto atende aos requisitos de IAM.

## Prompt para o Canva AI

Crie uma apresentacao academica e moderna, com visual limpo, sobre um projeto em C# chamado "Portal IAM Docs". O projeto e um Sistema de Gestao de Documentos com IAM. A apresentacao deve ter 10 slides, com linguagem simples e tecnica, usando diagramas, fluxos e tabelas. O estilo deve ser profissional, com cores associadas a seguranca, tecnologia e governanca.

## Slide 1 - Titulo

**Titulo:** Portal IAM Docs

**Subtitulo:** Sistema de Gestao de Documentos com Identidade e Acesso

**Conteudo:**

- Projeto pratico de IAM
- C#, ASP.NET Core Web API
- LDAP, RBAC, OIDC, OAuth2 M2M e SQLite

**Visual sugerido:** fundo tecnologico simples, icones de seguranca, usuario e documentos.

## Slide 2 - Objetivo do Projeto

**Titulo:** Objetivo

**Conteudo:**

- Criar um portal de documentos com controle de acesso.
- Autenticar usuarios em um servidor LDAP real na AWS EC2.
- Aplicar permissoes usando RBAC.
- Exportar documentos para Google Drive usando OpenID Connect.
- Demonstrar integracao tecnica por OAuth2 Machine-to-Machine.
- Registrar eventos em auditoria.

**Fala:** O foco do projeto nao e uma regra de negocio complexa, mas sim demonstrar autenticacao, autorizacao e governanca.

## Slide 3 - Arquitetura Geral

**Titulo:** Arquitetura da Aplicacao

**Conteudo:**

| Camada | Funcao |
|---|---|
| Front | HTML, CSS e Bootstrap |
| Controllers/API | Endpoints e validacao de acesso |
| Core | Servicos, interfaces, DTOs e RBAC |
| LDAP EC2 | Usuarios, senhas e grupos |
| SQLite | Metadados dos documentos e auditoria |
| Storage | Arquivos fisicos |
| Google Drive | Exportacao externa |

**Visual sugerido:** diagrama em blocos: Front -> API -> Core -> LDAP/SQLite/Storage/Google.

## Slide 4 - Login LDAP

**Titulo:** Autenticacao Centralizada por LDAP

**Conteudo:**

- O usuario informa login e senha.
- A aplicacao consulta o servidor LDAP na EC2.
- O sistema faz bind LDAP com o usuario.
- Se a senha estiver correta, a aplicacao cria uma sessao por cookie.
- Os grupos LDAP viram papeis RBAC.

**Usuarios de teste:**

- admin -> Administrador
- gestor -> Gestor
- aluno -> Usuario
- auditor -> Auditor

**Fala:** O SQLite nao guarda usuario nem senha. A autenticacao real acontece no LDAP.

## Slide 5 - RBAC: Papeis e Permissoes

**Titulo:** Controle de Acesso Baseado em Papeis

**Conteudo:**

| Papel | Permissoes principais |
|---|---|
| Administrador | Controle total |
| Gestor | Documentos e Google Drive |
| Usuario | Apenas documentos Publico |
| Auditor | Apenas auditoria |
| ServicoM2M | Exportacao tecnica por token |

**Visual sugerido:** matriz ou diagrama ligando Papel -> Permissao -> Recurso.

**Fala:** O acesso nao e decidido pela tela. O back-end valida cada permissao antes de executar a acao.

## Slide 6 - Regras de Documentos

**Titulo:** Regras de Acesso aos Documentos

**Conteudo:**

| Papel | Enviar | Visualizar/Baixar |
|---|---|---|
| Administrador | Publico, Interno, Confidencial | Todos |
| Gestor | Publico, Interno, Confidencial | Todos |
| Usuario | Apenas Publico | Apenas Publico |
| Auditor | Nao envia | Nao visualiza documentos |

**Fala:** O aluno nao pode ver documento confidencial. Mesmo que tente acessar pela API, o back-end bloqueia.

## Slide 7 - Google OIDC e Drive

**Titulo:** OpenID Connect com Google

**Conteudo:**

- O usuario primeiro faz login pelo LDAP.
- Depois conecta uma conta Google.
- O Google autentica a conta externa por OIDC.
- A aplicacao usa o token para exportar arquivo para Google Drive.
- O escopo usado e `drive.file`.

**Fala:** O Google nao substitui o login LDAP. Ele e usado para autorizar a exportacao para o Drive.

## Slide 8 - OAuth2 Machine-to-Machine

**Titulo:** Exportacao M2M com OAuth2

**Conteudo:**

- Um cliente tecnico usa `client_id` e `client_secret`.
- A API emite um Bearer token.
- O cliente usa o token para exportar um documento.
- O arquivo e copiado para um storage externo simulado.
- Esse fluxo nao depende de usuario humano logado.

**Fala:** M2M significa maquina falando com maquina. No projeto, representa uma integracao externa autorizada por token.

## Slide 9 - Auditoria e Governanca

**Titulo:** Auditoria e Governanca

**Conteudo:**

Eventos registrados:

- login LDAP com sucesso ou falha;
- logout;
- upload e download;
- exclusao de documento;
- troca de papel no LDAP;
- exportacao Google Drive;
- emissao de token M2M;
- exportacao M2M.

**Governanca:**

- Apenas Administrador altera papeis.
- A troca acontece no LDAP.
- O usuario precisa fazer novo login para receber o novo papel.

## Slide 10 - Demonstracao e Conclusao

**Titulo:** Resultado Final

**Conteudo:**

- Aplicacao integrada a LDAP real na AWS EC2.
- RBAC aplicado no front e no back-end.
- Google OIDC e Drive funcionando como integracao externa.
- OAuth2 M2M demonstrando acesso tecnico.
- SQLite registrando documentos e auditoria.
- Swagger documentando e testando os endpoints.

**Fala final:** O projeto demonstra os principais conceitos de IAM: autenticacao centralizada, autorizacao por papeis, integracao federada, autorizacao entre sistemas e auditoria.

## Slide Extra - Fluxo RBAC

Use se quiser incluir um slide visual extra.

**Titulo:** Fluxo de Autorizacao RBAC

**Conteudo:**

1. Usuario faz login.
2. LDAP valida senha.
3. LDAP retorna grupos.
4. Grupos viram papeis.
5. Papeis viram permissoes.
6. Controller valida permissao.
7. Acao e liberada ou negada.
8. Auditoria registra evento.

**Frase:** LDAP responde quem e o usuario. RBAC responde o que ele pode fazer.
