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

## Status

DotRadar is currently an early preview.

## License

MIT
