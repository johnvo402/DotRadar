# Getting started

DotRadar scans .NET projects and solutions for production-related
reliability, performance, security, and maintainability issues.

## Requirements

- .NET 10 SDK or later.
- A project supported by MSBuild.
- Windows, Linux, or macOS.

## Install from NuGet

After the first public release:

```powershell
dotnet tool install --global DotRadar.Tool
```

Verify:

```powershell
dotradar --version
dotradar list-rules
```

## Install a locally packed version

From the DotRadar repository:

```powershell
dotnet pack `
  src/DotRadar.Tool/DotRadar.Tool.csproj `
  -c Release `
  -o artifacts/packages
```

```powershell
dotnet tool install `
  --global DotRadar.Tool `
  --version 0.1.0 `
  --add-source artifacts/packages
```

## Scan a project

```powershell
dotradar scan src/MyApplication/MyApplication.csproj
```

Scan a solution:

```powershell
dotradar scan MyApplication.sln
```

Scan a directory:

```powershell
dotradar scan .
```

When a directory is supplied, DotRadar looks for a solution or project
inside that directory.

## Output formats

Human-readable output:

```powershell
dotradar scan . --format text
```

Machine-readable JSON:

```powershell
dotradar scan . --format json
```

SARIF 2.1.0:

```powershell
dotradar scan . --format sarif > dotradar.sarif
```

## Failure threshold

DotRadar fails on warnings and errors by default:

```powershell
dotradar scan . --fail-on warning
```

Only fail on errors:

```powershell
dotradar scan . --fail-on error
```

## Exit codes

| Code | Meaning                                                    |
| ---: | ---------------------------------------------------------- |
|    0 | Scan completed without diagnostics meeting the threshold   |
|    1 | Invalid arguments or configuration                         |
|    2 | One or more diagnostics met the failure threshold          |
|    3 | Project, solution, config, or baseline could not be loaded |
|    4 | Unexpected internal error                                  |
|  130 | Operation cancelled                                        |

## Next steps

- [Configuration](configuration.md)
- [Baselines](baseline.md)
- [CI integration](ci.md)
- [Rule catalog](rules/)
