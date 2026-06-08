# Diagrama RBAC

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
    R1 --> P5["exports.oidc"]
    R1 --> P6["exports.m2m"]
    R1 --> P7["users.manage.roles"]
    R1 --> P8["audit.view"]

    R2 --> P1
    R2 --> P2
    R2 --> P3
    R2 --> P5
    R2 --> P8

    R3 --> P9["documents.view.own"]
    R3 --> P10["documents.download.own"]
    R3 --> P1
    R3 --> P5

    R4 --> P2
    R4 --> P8

    R5 --> P6

    P1 --> D1["Recurso: Documento"]
    P2 --> D1
    P3 --> D1
    P4 --> D1
    P9 --> D1
    P10 --> D1
    P5 --> E1["Recurso: Exportacao Google Drive OIDC"]
    P6 --> E2["Recurso: Storage externo M2M"]
    P7 --> G1["Recurso: Diretorio LDAP demo"]
    P8 --> A1["Recurso: Auditoria"]
```

## Resumo dos papeis

| Papel | Recursos | Permissoes principais |
|---|---|---|
| Administrador | Documentos, usuarios, auditoria, exportacoes | Controle total e governanca |
| Gestor | Documentos e auditoria | Ver/baixar todos, exportar OIDC, auditar |
| Usuario | Proprios documentos | Upload, ver/baixar proprios, exportar OIDC |
| Auditor | Documentos e auditoria | Consulta sem alteracao |
| ServicoM2M | Storage externo | Exportacao por OAuth2 client credentials |
