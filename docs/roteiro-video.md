# Roteiro do video de demonstracao

Duracao sugerida: 3 a 5 minutos.

1. Abrir a pagina inicial e explicar que o sistema e um portal de documentos com IAM.
2. Mostrar a matriz RBAC na tela inicial.
3. Fazer login como `admin / Admin@123`.
4. Enviar um documento em `Documentos`.
5. Mostrar que o admin tem opcoes de baixar, exportar OIDC e excluir.
6. Ir em `Usuarios` e alterar o papel do usuario `aluno` para `Gestor`.
7. Sair e entrar como `aluno / Aluno@123`.
8. Mostrar que as permissoes mudaram apos a troca de papel no diretorio.
9. Exportar um documento via OIDC, preenchendo a conta Google demo.
10. Demonstrar OAuth2 M2M no PowerShell:

```powershell
$token = Invoke-RestMethod -Method Post -Uri "http://localhost:5169/api/oauth/token" -ContentType "application/x-www-form-urlencoded" -Body "client_id=storage-client&client_secret=M2M@123"
Invoke-RestMethod -Method Post -Uri "http://localhost:5169/api/m2m/export/1" -Headers @{ Authorization = "Bearer $($token.access_token)" }
```

11. Abrir `Auditoria` e mostrar eventos de login, upload, troca de papel, OIDC e M2M.
12. Encerrar mostrando as pastas `Storage/external/oidc-google-drive` e `Storage/external/m2m-storage`.
