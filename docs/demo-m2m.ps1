$token = Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5169/api/oauth/token" `
  -ContentType "application/x-www-form-urlencoded" `
  -Body "client_id=storage-client&client_secret=M2M@123"

Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5169/api/m2m/export/1" `
  -Headers @{ Authorization = "Bearer $($token.access_token)" }
