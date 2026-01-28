# Development

Setup for local development.

## Run

```sh
cd src/
dotnet watch run
```

## Database

```sh
cd src/
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=markettrust;User Id=SA;Password=<devPassword>;Encrypt=false;TrustServerCertificate=True"
dotnet ef migrations add Init
dotnet ef database update
dotnet ef database drop
dotnet ef migrations remove
```

## JWT

```sh
dotnet user-secrets set "JWT:Issuer" "http://localhost:5167"
dotnet user-secrets set "JWT:Audience" "http://localhost:5167"
dotnet user-secrets set "JWT:SigningKey" "devSigningKey"
```

## CORS

```sh
dotnet user-secrets set "AllowedOrigins" "http://localhost:5173"
```
