# CI integration

DotRadar supports text, JSON, and SARIF output and uses exit codes
designed for continuous integration.

## Basic CI command

```powershell
dotradar scan . --fail-on warning
```

The command returns exit code `2` when a warning or error is found.

Only block errors:

```powershell
dotradar scan . --fail-on error
```

## JSON report

```powershell
dotradar scan . `
  --format json `
  --fail-on error `
  1> dotradar-report.json
```

JSON includes:

- `diagnosticCount`
- `suppressedCount`
- `failureThreshold`
- `failureCount`
- `diagnostics`

## SARIF report

```powershell
dotradar scan . `
  --format sarif `
  --fail-on error `
  1> dotradar.sarif
```

SARIF output uses version 2.1.0 and includes source locations, rule
metadata, severity, and partial fingerprints.

## GitHub Actions

```yaml
name: DotRadar

on:
  push:
    branches:
      - main
  pull_request:
    branches:
      - main

permissions:
  contents: read
  actions: read
  security-events: write

jobs:
  analyze:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v6

      - name: Setup .NET
        uses: actions/setup-dotnet@v6
        with:
          dotnet-version: 10.0.x

      - name: Install DotRadar
        run: >
          dotnet tool install
          --global DotRadar.Tool

      - name: Run DotRadar
        run: >
          dotradar scan .
          --format sarif
          --fail-on error
          > dotradar.sarif

      - name: Upload SARIF
        if: always()
        uses: github/codeql-action/upload-sarif@v4
        with:
          sarif_file: dotradar.sarif
          category: dotradar
```

## CI with a baseline

```yaml
- name: Run DotRadar
  run: >
    dotradar scan .
    --baseline .dotradar-baseline.json
    --format sarif
    --fail-on warning
    > dotradar.sarif
```

The baseline should already exist in the repository and should not be
regenerated during the workflow.

## Private repositories

GitHub SARIF upload for private repositories may require GitHub Code
Security to be enabled.

If Code Scanning is unavailable, retain the SARIF or JSON file as a
workflow artifact instead.

## Recommended policy

For a new project:

```powershell
dotradar scan . --fail-on warning
```

For a legacy project:

```powershell
dotradar scan . `
  --baseline .dotradar-baseline.json `
  --fail-on warning
```

For gradual adoption:

```powershell
dotradar scan . --fail-on error
```
