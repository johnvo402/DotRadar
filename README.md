# DotRadar

Production diagnostics for .NET projects.

DotRadar scans .NET projects and solutions for reliability,
performance, security, and maintainability issues.

## Installation

```powershell
dotnet tool install --global DotRadar.Tool
```

## Usage

```powershell
dotradar scan .
dotradar list-rules
dotradar baseline .
```

## Output formats

```powershell
dotradar scan . --format text
dotradar scan . --format json
dotradar scan . --format sarif
```

## Configuration

Create `.dotradar.json`:

```json
{
  "rules": {
    "DTR1101": {
      "enabled": true,
      "severity": "error"
    },
    "DTR1102": {
      "enabled": false
    }
  }
}
```
## Rules

| Rule | Description | Default severity |
|---|---|---|
| [DTR1101](docs/rules/DTR1101.md) | Avoid blocking on asynchronous operations | Warning |
| [DTR1102](docs/rules/DTR1102.md) | CancellationToken parameter is not used | Warning |

## Documentation

- [Configuration](docs/configuration.md)
- [Baselines](docs/baseline.md)
- [CI integration](docs/ci.md)

## Status

DotRadar is currently an early preview.

## License

MIT
