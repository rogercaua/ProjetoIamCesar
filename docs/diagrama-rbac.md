# Diagrama RBAC

Este artefato representa a matriz RBAC da aplicacao. Ele mostra:

- quais identidades existem no LDAP;
- qual papel cada identidade recebe;
- quais permissoes cada papel possui;
- quais recursos cada permissao protege.

```mermaid
flowchart LR
    subgraph Atores["Identidades e grupos LDAP"]
        U1["admin\nGrupo LDAP: Administradores"]
        U2["gestor\nGrupo LDAP: Gestores"]
        U3["aluno\nGrupo LDAP: Usuarios"]
        U4["auditor\nGrupo LDAP: Auditores"]
        C1["storage-client\nCliente tecnico M2M"]
    end

    subgraph Papeis["Papeis RBAC"]
        R1["Administrador"]
        R2["Gestor"]
        R3["Usuario"]
        R4["Auditor"]
        R5["ServicoM2M"]
    end

    subgraph Permissoes["Permissoes efetivas"]
        P1["Enviar documento\nPublico, Interno ou Confidencial"]
        P2["Ver todos os documentos"]
        P3["Baixar todos os documentos"]
        P4["Excluir documento"]
        P5["Exportar documentos permitidos\npara Google Drive"]
        P6["Exportar via OAuth2 M2M"]
        P7["Alterar papel de usuario\nno LDAP"]
        P8["Consultar auditoria"]
        P9["Enviar documento\nsomente Publico"]
        P10["Ver documentos Publico"]
        P11["Baixar documentos Publico"]
    end

    subgraph Recursos["Recursos protegidos"]
        D1["Documentos\nStorage/Documents"]
        D2["Metadados\nSQLite"]
        G1["Google Drive API"]
        S1["Storage externo\nM2M"]
        L1["Diretorio LDAP\nEC2"]
        A1["Logs de auditoria\nSQLite"]
    end

    U1 --> R1
    U2 --> R2
    U3 --> R3
    U4 --> R4
    C1 --> R5

    R1 --> P1
    R1 --> P2
    R1 --> P3
    R1 --> P4
    R1 --> P5
    R1 --> P6
    R1 --> P7
    R1 --> P8

    R2 --> P1
    R2 --> P2
    R2 --> P3
    R2 --> P5

    R3 --> P9
    R3 --> P10
    R3 --> P11
    R3 --> P5

    R4 --> P8
    R5 --> P6

    P1 --> D1
    P1 --> D2
    P2 --> D2
    P2 --> D1
    P3 --> D1
    P4 --> D1
    P4 --> D2
    P5 --> G1
    P6 --> S1
    P7 --> L1
    P8 --> A1
    P9 --> D1
    P9 --> D2
    P10 --> D2
    P10 --> D1
    P11 --> D1
```

## Resumo dos papeis

| Papel | Origem | Recursos | Permissoes principais |
|---|---|---|---|
| Administrador | Grupo LDAP `Administradores` | Documentos, usuarios, auditoria e exportacoes | Controle total |
| Gestor | Grupo LDAP `Gestores` | Documentos e Google Drive | Envia qualquer classificacao, ve/baixa todos, exporta para Drive |
| Usuario | Grupo LDAP `Usuarios` | Documentos publicos e Google Drive | Envia apenas Publico, ve/baixa Publico, exporta Publico para Drive |
| Auditor | Grupo LDAP `Auditores` | Auditoria | Consulta logs sem alterar dados |
| ServicoM2M | `client_id` tecnico | Storage externo | Exporta por OAuth2 Client Credentials |

## Matriz de permissoes

| Permissao efetiva | Administrador | Gestor | Usuario | Auditor | ServicoM2M |
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
| Alterar papeis no LDAP | Sim | Nao | Nao | Nao | Nao |
| Consultar auditoria | Sim | Nao | Nao | Sim | Nao |
| Exportar via OAuth2 M2M | Sim | Nao | Nao | Nao | Sim |

## Como explicar na apresentacao

O LDAP autentica o usuario e informa o grupo. A aplicacao converte o grupo LDAP em um papel RBAC. Cada papel possui permissoes especificas, e os controllers validam essas permissoes antes de liberar recursos como documentos, Google Drive, auditoria, LDAP e storage M2M.

Exemplo:

- `aluno` pertence ao grupo LDAP `Usuarios`.
- Esse grupo vira o papel `Usuario`.
- O papel `Usuario` permite apenas documentos `Publico`.
- Mesmo que o aluno tente acessar um documento `Confidencial` pela API, o back-end retorna acesso negado.

## Onde isso esta no codigo

| Parte | Arquivo |
|---|---|
| Nomes dos papeis | `Back/Core/Models/RbacDefinition.cs` |
| Nomes das permissoes | `Back/Core/Models/RbacDefinition.cs` |
| Matriz role -> permissoes | `Back/Core/Services/RbacService.cs` |
| Restricao de classificacao no upload | `Back/Core/Services/RbacService.cs` e `Back/Controllers/DocumentsController.cs` |
| Filtro de visualizacao/download | `Back/Core/Services/RbacService.cs` |
| Tela por permissao | `Back/Controllers/PortalController.cs` |
| Auditoria | `Back/Controllers/AuditController.cs` |
| Troca de papel no LDAP | `Back/Controllers/UsersController.cs` |
