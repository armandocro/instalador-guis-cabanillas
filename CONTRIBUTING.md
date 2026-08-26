# Contributing to Instalador de GUIs

## Development Setup
1. Install .NET 8 SDK
2. Clone the repository
3. Open `InstaladorGuis.sln` in Visual Studio 2022 or later

## Building
```bash
dotnet build
```

## Testing
```bash
dotnet test
```

## Code Style
- Follow `.editorconfig` rules
- Use file-scoped namespaces
- Prefer `var` when type is evident
- Use `BrushHelper.FromHex()` for brush creation

## Pull Request Process
1. Create a feature branch from `develop`
2. Make your changes
3. Ensure all tests pass
4. Update `CHANGELOG.md` if applicable
5. Request review from `@inditex/logistica-dev`

## Security
- Do not hardcode secrets or credentials
- Do not commit local center-specific overrides (`*.local.json`)
- Network paths in `Brands/*.json` are center-specific templates; adjust per site as needed
- Run `detect-secrets scan` before committing
