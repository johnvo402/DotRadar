# Configuration

DotRadar can be configured using a `.dotradar.json` file.

## Basic configuration

```json
{
  "$schema": "https://raw.githubusercontent.com/johnvo402/DotRadar/main/schemas/dotradar.schema.json",
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

## Configuration discovery

When `--config` is not specified, DotRadar starts from the scan target
directory and searches parent directories for the nearest
`.dotradar.json`.

For example:

```text
repository/
├── .dotradar.json
├── src/
│   └── Application/
│       └── Application.csproj
└── tests/
```

Running:

```powershell
dotradar scan src/Application
```

uses:

```text
repository/.dotradar.json
```

The nearest configuration file wins. DotRadar does not merge multiple
configuration files.

## Explicit configuration

Use a specific configuration file:

```powershell
dotradar scan . `
  --config configs/strict.json
```

An explicit `--config` path takes precedence over automatic discovery.

## Enable or disable a rule

```json
{
  "rules": {
    "DTR1101": {
      "enabled": false
    }
  }
}
```

Rules are enabled by default.

## Override severity

```json
{
  "rules": {
    "DTR1101": {
      "severity": "error"
    }
  }
}
```

Supported values:

- `info`
- `warning`
- `error`

Severity configuration changes report severity and participates in
`--fail-on` evaluation.

## Failure threshold

The failure threshold is a command-line option, not a rule setting:

```powershell
dotradar scan . --fail-on error
```

A warning is still included in the report, but it does not cause exit
code `2` when the threshold is `error`.

## Strict validation

DotRadar rejects unknown rule IDs and misspelled properties.

This configuration is invalid:

```json
{
  "rules": {
    "DTR1101": {
      "severty": "error"
    }
  }
}
```

DotRadar reports:

```text
Configuration error:
Unknown property 'severty' in rule 'DTR1101'.
```

## JSON Schema

The schema is available at:

```text
schemas/dotradar.schema.json
```

Editors such as Visual Studio, VS Code, and Rider can use the `$schema`
property for validation and completion.
