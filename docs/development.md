# Development

## Requirements

- Windows 11.
- .NET 10 SDK.
- Git.

## Build

```powershell
dotnet restore
dotnet build --configuration Release --no-restore
```

## Test

```powershell
dotnet test --configuration Release --no-build
```

The primary target architecture is x64.

## Branches

- `main`: stable.
- `devel`: active development.

No other permanent branches should be used.

## Policy

Keep User Mode and System Mode separated by milestone. Do not add service behavior to v0.1 code paths. Failures must be visible and structured.
