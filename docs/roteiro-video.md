# Roteiro do video de demonstracao

Duracao sugerida: 3 a 5 minutos.

## Roteiro objetivo

1. Abrir `http://localhost:5169` e mostrar que o front usa HTML/CSS/Bootstrap, sem Razor.
2. Fazer login com `admin / Admin@123`.
3. Explicar que o login chama o LDAP da EC2, nao o SQLite.
4. Abrir `/api/auth/me` no Swagger e mostrar roles/permissoes carregadas no cookie.
5. Enviar um documento e explicar que:
   - metadados ficam em `Storage/iam-documents.db`;
   - arquivo fisico fica em `Storage/Documents`.
6. Entrar como `aluno / Aluno@123` e mostrar que ele so ve documentos Publico e so consegue enviar Publico.
7. Entrar como `gestor / Gestor@123` e mostrar que ele ve documentos, mas nao ve auditoria nem usuarios.
8. Entrar como `auditor / Auditor@123` e mostrar que ele ve apenas auditoria.
9. Voltar como `admin` e alterar o papel de um usuario no painel LDAP.
10. Conectar uma conta Google pelo botao `Conectar Google`.
11. Exportar um documento para Google Drive.
12. Abrir `/swagger` e mostrar endpoints documentados.
13. Demonstrar OAuth2 M2M no PowerShell:

```powershell
$token = Invoke-RestMethod -Method Post -Uri "http://localhost:5169/api/oauth/token" -ContentType "application/x-www-form-urlencoded" -Body "client_id=storage-client&client_secret=M2M@123"
Invoke-RestMethod -Method Post -Uri "http://localhost:5169/api/m2m/export/1" -Headers @{ Authorization = "Bearer $($token.access_token)" }
```

14. Abrir a auditoria e mostrar eventos de login, upload, troca de papel, Google Drive e M2M.
15. Encerrar mostrando `Storage/iam-documents.db`, `Storage/Documents` e `Storage/external/m2m-storage`.

## Frases curtas para explicar

**LDAP**

> O usuario e senha sao validados no servidor LDAP da EC2. O banco local nao guarda senha.

**RBAC**

> O grupo LDAP vira papel na aplicacao, e o papel vira lista de permissoes.

**SQLite**

> O SQLite guarda documentos e auditoria, nao identidade.

**OIDC/Google**

> O Google e usado para conectar uma conta externa e autorizar envio para Drive. O login principal continua sendo LDAP.

**OAuth2 M2M**

> O M2M representa uma integracao entre sistemas usando client_id, client_secret e Bearer token.
