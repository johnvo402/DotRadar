# DotRadar

[![CI](https://github.com/johnvo402/DotRadar/actions/workflows/ci.yml/badge.svg)](https://github.com/johnvo402/DotRadar/actions/workflows/ci.yml)
[![DotRadar Analysis](https://github.com/johnvo402/DotRadar/actions/workflows/dotradar.yml/badge.svg)](https://github.com/johnvo402/DotRadar/actions/workflows/dotradar.yml)

Production diagnostics for .NET projects.

DotRadar uses Roslyn and MSBuild to find reliability, performance,
security, and maintainability issues before they reach production.

> DotRadar is currently an early preview.

## Features

- Semantic analysis powered by Roslyn.
- Supports projects, solutions, and directories.
- Configurable rules and severity.
- Text, JSON, and SARIF 2.1.0 output.
- Baselines for gradual adoption.
- CI-friendly exit codes.
- Windows and Linux test coverage.

## Installation

After the first public NuGet release:

```powershell
dotnet tool install --global DotRadar.Tool
```

Verify:

```powershell
dotradar --version
dotradar list-rules
```

## Quick start

```powershell
dotradar scan .
```

Strict CI:

```powershell
dotradar scan . --fail-on warning
```

JSON:

```powershell
dotradar scan . --format json
```

SARIF:

```powershell
dotradar scan . --format sarif > dotradar.sarif
```

## Rules

| Rule                             | Description                               | Default severity |
| -------------------------------- | ----------------------------------------- | ---------------- |
| [DTR1101](docs/rules/DTR1101.md) | Avoid blocking on asynchronous operations | Warning          |
| [DTR1102](docs/rules/DTR1102.md) | CancellationToken parameter is not used   | Warning          |

## Configuration

Create `.dotradar.json`:

```json
{
  "$schema": "https://raw.githubusercontent.com/johnvo402/DotRadar/main/schemas/dotradar.schema.json",
  "rules": {
    "DTR1101": {
      "severity": "error"
    },
    "DTR1102": {
      "enabled": false
    }
  }
}
```

## Baseline

Record existing diagnostics:

```powershell
dotradar baseline .
```

Only report new diagnostics:

```powershell
dotradar scan . `
  --baseline .dotradar-baseline.json
```

## Documentation

- [Getting started](docs/getting-started.md)
- [Configuration](docs/configuration.md)
- [Baselines](docs/baseline.md)
- [CI integration](docs/ci.md)
- [Rule documentation](docs/rules/)

## Build from source

```powershell
dotnet restore
dotnet build
dotnet test
```

Pack:

```powershell
dotnet pack `
  src/DotRadar.Tool/DotRadar.Tool.csproj `
  -c Release `
  -o artifacts/packages
```

## Roadmap

- Additional reliability and performance rules.
- Inline suppression.
- Configuration inheritance.
- IDE analyzer package.
- Automated code fixes.
- HTML reports.

## Contributing

Issues and pull requests are welcome. Contribution guidelines will be
added before the first stable release.

## License

MIT
