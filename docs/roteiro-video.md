# Roteiro do video de demonstracao

Duracao sugerida: 3 a 5 minutos.

1. Abrir `http://localhost:5169` e mostrar que o front e HTML/CSS sem Razor.
2. Fazer login com um usuario existente no LDAP do EC2.
3. Enviar um documento e explicar que o arquivo fica em `Storage/Documents`.
4. Mostrar que os botoes mudam conforme o papel RBAC do usuario.
5. Entrar como Administrador e alterar o papel de um usuario pelo endpoint/tela de usuarios LDAP.
6. Conectar uma conta Google pelo botao de conexao OIDC.
7. Exportar um documento para Google Drive.
8. Abrir `/swagger` e mostrar os endpoints documentados.
9. Demonstrar OAuth2 M2M no PowerShell:

```powershell
$token = Invoke-RestMethod -Method Post -Uri "http://localhost:5169/api/oauth/token" -ContentType "application/x-www-form-urlencoded" -Body "client_id=storage-client&client_secret=M2M@123"
Invoke-RestMethod -Method Post -Uri "http://localhost:5169/api/m2m/export/1" -Headers @{ Authorization = "Bearer $($token.access_token)" }
```

10. Abrir a auditoria e mostrar eventos de login, upload, troca de papel, Google Drive e M2M.
11. Encerrar mostrando `Storage/iam-documents.db`, `Storage/Documents` e `Storage/external/m2m-storage`.
