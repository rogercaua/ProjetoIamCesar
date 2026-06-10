# Fluxograma RBAC

Este fluxograma mostra como a aplicacao decide o que cada usuario pode acessar.

```mermaid
flowchart TD
    A["Usuario informa login e senha"] --> B["AuthController recebe POST /api/auth/login"]
    B --> C["LdapDirectoryService consulta LDAP da EC2"]
    C --> D{"Bind LDAP valido?"}
    D -->|Nao| E["Login negado e auditoria registra ldap.login.failed"]
    D -->|Sim| F["Sistema busca grupos LDAP do usuario"]
    F --> G["Grupos viram roles da aplicacao"]
    G --> H["Cookie recebe claims de usuario e roles"]
    H --> I["Usuario acessa dashboard ou API"]
    I --> J["RbacService transforma roles em permissoes"]
    J --> K{"Tem permissao para a acao?"}
    K -->|Nao| L["API retorna 403 ou tela esconde a opcao"]
    K -->|Sim| M["Controller executa a acao"]
    M --> N["AuditService grava evento no SQLite"]
```

## Decisao por papel

```mermaid
flowchart LR
    Login["Login LDAP aprovado"] --> Role{"Role no cookie"}

    Role -->|Administrador| Admin["Documentos + usuarios LDAP + auditoria + exportacoes"]
    Role -->|Gestor| Gestor["Documentos + upload + Google Drive"]
    Role -->|Usuario| Usuario["Upload Publico + documentos Publico + Google Drive"]
    Role -->|Auditor| Auditor["Somente auditoria"]
    Role -->|ServicoM2M| M2M["Somente exportacao por token OAuth2"]

    Admin --> API["Controllers validam permissoes"]
    Gestor --> API
    Usuario --> API
    Auditor --> API
    M2M --> API

    API --> Permitido["Acao permitida"]
    API --> Negado["Acao negada"]
```

## Matriz de permissoes

| Permissao | Administrador | Gestor | Usuario | Auditor | ServicoM2M |
|---|---:|---:|---:|---:|---:|
| `documents.upload` | Sim | Sim | Sim | Nao | Nao |
| `documents.view.public` | Nao | Nao | Sim | Nao | Nao |
| `documents.view.all` | Sim | Sim | Nao | Nao | Nao |
| `documents.download.public` | Nao | Nao | Sim | Nao | Nao |
| `documents.download.all` | Sim | Sim | Nao | Nao | Nao |
| `documents.delete` | Sim | Nao | Nao | Nao | Nao |
| `exports.google_drive` | Sim | Sim | Sim | Nao | Nao |
| `exports.m2m` | Sim | Nao | Nao | Nao | Sim |
| `users.manage.roles` | Sim | Nao | Nao | Nao | Nao |
| `audit.view` | Sim | Nao | Nao | Sim | Nao |

## Como ler o fluxograma

1. Autenticacao responde: quem e o usuario?
2. LDAP responde: a senha esta correta e quais grupos o usuario possui?
3. RBAC responde: o papel permite essa acao?
4. Controller executa ou nega.
5. Auditoria registra o que aconteceu.

Frase curta para a apresentacao:

> O LDAP autentica e informa os grupos. O RBAC transforma esses grupos em permissoes. Os controllers aplicam essas permissoes antes de executar qualquer acao sensivel.
