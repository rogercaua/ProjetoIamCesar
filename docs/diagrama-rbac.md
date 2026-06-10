# Diagrama RBAC

Este diagrama mostra a relacao entre usuarios, papeis, permissoes e recursos protegidos. Ele complementa o `docs/fluxograma-rbac.md`: o fluxograma mostra a decisao no tempo de execucao; este diagrama mostra a matriz de acesso.

```mermaid
flowchart LR
    U1["admin"] --> R1["Administrador"]
    U2["gestor"] --> R2["Gestor"]
    U3["aluno"] --> R3["Usuario"]
    U4["auditor"] --> R4["Auditor"]
    C1["storage-client"] --> R5["ServicoM2M"]

    R1 --> P1["documents.upload"]
    R1 --> P2["documents.view.all"]
    R1 --> P3["documents.download.all"]
    R1 --> P4["documents.delete"]
    R1 --> P5["exports.google_drive"]
    R1 --> P6["exports.m2m"]
    R1 --> P7["users.manage.roles"]
    R1 --> P8["audit.view"]

    R2 --> P1
    R2 --> P2
    R2 --> P3
    R2 --> P5

    R3 --> P9["documents.view.own"]
    R3 --> P10["documents.download.own"]
    R3 --> P1
    R3 --> P5

    R4 --> P8

    R5 --> P6

    P1 --> D1["Recurso: Documento"]
    P2 --> D1
    P3 --> D1
    P4 --> D1
    P9 --> D1
    P10 --> D1
    P5 --> E1["Recurso: Google Drive API"]
    P6 --> E2["Recurso: Storage externo M2M"]
    P7 --> G1["Recurso: Diretorio LDAP EC2"]
    P8 --> A1["Recurso: Auditoria SQLite"]
```

## Resumo dos papeis

| Papel | Recursos | Permissoes principais |
|---|---|---|
| Administrador | Documentos, usuarios, auditoria, exportacoes | Controle total e governanca |
| Gestor | Documentos e exportacoes | Ver/baixar todos e exportar para Google Drive |
| Usuario | Proprios documentos | Upload, ver/baixar proprios, exportar para Google Drive |
| Auditor | Auditoria | Consulta logs sem alterar dados |
| ServicoM2M | Storage externo | Exportacao por OAuth2 client credentials |

## Observacoes importantes

- `Administrador` e o unico papel humano com controle total.
- `Gestor` gerencia documentos, mas nao consulta auditoria e nao altera papeis.
- `Usuario` comum so acessa documentos em que ele e o dono.
- `Auditor` nao acessa documentos; ele apenas consulta logs.
- `ServicoM2M` nao representa uma pessoa logada. Ele representa uma integracao tecnica autenticada por token OAuth2.

## Onde isso esta no codigo

| Parte | Arquivo |
|---|---|
| Nomes dos papeis | `Back/Core/Models/RbacDefinition.cs` |
| Nomes das permissoes | `Back/Core/Models/RbacDefinition.cs` |
| Matriz role -> permissoes | `Back/Core/Services/RbacService.cs` |
| Validacao nos documentos | `Back/Controllers/DocumentsController.cs` |
| Validacao na auditoria | `Back/Controllers/AuditController.cs` |
| Validacao na troca de papel | `Back/Controllers/UsersController.cs` |
