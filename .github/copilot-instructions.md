# ArcGIS Pro CLI - Copilot Instructions

## Project Overview

This project provides AI agents visibility into ArcGIS Pro sessions through two components:

1. **ProExporter (C# Add-in)** - Runs inside ArcGIS Pro, exports session data
2. **cli (Python CLI)** - Queries exported data from the command line

**Key principle: The add-in exports, the CLI queries.** Keep these concerns separate.

## Architecture

```
arcgispro_cli/
├── ProExporter/     # C# ArcGIS Pro add-in (exports data)
│   ├── *.cs         # Button handlers, context collection, serialization
│   ├── Config.daml  # Add-in UI configuration (includes version)
│   └── *.csproj     # MSBuild project (NOT dotnet CLI)
├── cli/             # Python CLI (queries data)
│   ├── arcgispro_cli/
│   │   ├── cli.py       # Main entry point
│   │   └── commands/    # Click command modules
│   └── pyproject.toml   # Hatchling build config (includes version)
└── .github/         # CI/CD workflows
```

## Versioning

**CLI and add-in versions must match.** Update both when releasing:
- `cli/pyproject.toml` → `version = "X.Y.Z"`
- `ProExporter/Config.daml` → `<AddInInfo ... version="X.Y.Z" ...>`

## Build Instructions

### Add-in (ProExporter)

- **Requires**: Visual Studio 2022 with ArcGIS Pro SDK, .NET 8 SDK
- **Build command**: Use MSBuild, not `dotnet build`
  ```
  "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ProExporter.sln /p:Configuration=Release
  ```
- **Output**: `.esriAddinX` file in `bin/Release/`
- **Why not dotnet CLI**: The Pro SDK uses MSBuild tasks incompatible with .NET Core MSBuild

### CLI (Python)

- **Requires**: Python 3.9+
- **Install for development**: `pip install -e .` from `cli/` directory
- **Build for release**: `python -m build` (uses hatchling)
- **Run tests**: `pytest` from `cli/` directory

## Commit Conventions

Use conventional commits with short, clear messages:

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation only
- `chore:` - Maintenance tasks
- `feat!:` or `fix!:` - Breaking change

**Rules:**
- Subject line under 50 characters
- Use scope when helpful: `feat(addin):`, `fix(cli):`
- Imperative mood: "add feature" not "added feature"
- No period at end of subject

**Examples:**
```
feat: add launch command
feat(addin): add Terminal button to open cmd with Pro Python env
fix: handle missing .arcgispro folder gracefully
docs: update CLI command reference
```

## Python Code Style (cli/)

- Use type hints for function signatures
- Use Click decorators for CLI commands
- Use Rich for formatted console output
- Prefer `Path` over string paths
- Keep commands focused and single-purpose

```python
# Good
@click.command("layers")
@click.option("--json", "as_json", is_flag=True, help="Output as JSON")
def layers_cmd(as_json: bool) -> None:
    """List all layers in the project."""
    ...

# Avoid
def layers_cmd(as_json):  # Missing type hints
    ...
```

## C# Code Style (ProExporter/)

- Use XML doc comments for public classes and methods
- Use async/await for ArcGIS Pro API calls (required by Pro SDK)
- Run UI-blocking work via `QueuedTask.Run()`
- Handle exceptions gracefully with user-friendly messages

```csharp
// Good
/// <summary>
/// Exports map view as PNG image.
/// </summary>
public async Task<string> ExportMapAsync(MapView view, CancellationToken ct)
{
    return await QueuedTask.Run(() => { ... }, ct);
}
```

## Testing

- CLI tests live in `cli/tests/`
- Use pytest fixtures for common setup
- Test commands produce expected output format
- Add-in has no automated tests (manual testing in Pro)

## What NOT to Do

- Don't use `dotnet build` for the add-in (use MSBuild)
- Don't add arcpy imports to the CLI (it runs outside Pro)
- Don't mix export logic into CLI or query logic into add-in
- Don't commit `.arcgispro/` folders (they're project-specific output)
