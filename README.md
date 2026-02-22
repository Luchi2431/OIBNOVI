# OIBNOVI Console App

Small banking simulation with X.509 certificate authentication.

Prerequisites

- .NET 8 SDK
- OpenSSL (for generating test certificates)

Build & run

```bash
cd /Users/lukavlatkovic/Desktop/Luka/Fax/OIBNOVI
dotnet build
dotnet run --project OibnoviConsole.csproj
```

Run tests

```bash
dotnet test tests/Oibnovi.Tests/Oibnovi.Tests.csproj
```

Generate test certificates

```bash
./scripts/generate_certs.sh
```

Overview

- Roles: `Sluzbenik` (official) and `Korisnik` (customer)
- Auth: X.509 certificates (CN=username, OU=group)
- Persistence: JSON files (`accounts.json`, `users.json`, `transactions.json`)
- Logs: `events.log`, `transactions.log`

Quick notes

- To create a user from CLI the certificate CN must exactly match the username (case-sensitive).

For certs script:
chmod +x scripts/generate_certs.sh
./scripts/generate_certs.sh
