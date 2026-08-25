# Baselines

A baseline records existing diagnostics so a legacy project can adopt
DotRadar without fixing every issue immediately.

After creating a baseline, CI can fail only when new diagnostics are
introduced.

## Create a baseline

```powershell
dotradar baseline .
```

Default output:

```text
.dotradar-baseline.json
```

Choose another path:

```powershell
dotradar baseline . `
  --output config/dotradar-baseline.json
```

## Scan using a baseline

```powershell
dotradar scan . `
  --baseline .dotradar-baseline.json
```

Diagnostics found in the baseline are suppressed.

Example:

```text
No diagnostics found.
12 diagnostic(s) suppressed by baseline.
```

The command returns exit code `0` when no new diagnostic meets the
failure threshold.

## Combine baseline and configuration

Use the same configuration when generating and consuming a baseline:

```powershell
dotradar baseline . `
  --config .dotradar.json `
  --output .dotradar-baseline.json
```

```powershell
dotradar scan . `
  --config .dotradar.json `
  --baseline .dotradar-baseline.json
```

Disabled rules are not recorded in a newly generated baseline.

## Commit the baseline

For CI usage, commit the baseline file:

```powershell
git add .dotradar-baseline.json
git commit -m "chore: establish DotRadar baseline"
```

Baseline changes should be reviewed like source changes. Do not
regenerate a baseline automatically on every CI run, because that
would silently accept new diagnostics.

## Updating a baseline

Regenerate it deliberately after reviewing current diagnostics:

```powershell
dotradar baseline . `
  --output .dotradar-baseline.json
```

Then inspect:

```powershell
git diff -- .dotradar-baseline.json
```

## Fingerprints

A fingerprint currently uses:

- Rule ID.
- Source path relative to the scan root.
- Normalized source line.

This allows a diagnostic to remain matched when unrelated lines are
inserted above it.

Severity is not part of the fingerprint, so changing a rule from
`warning` to `error` does not invalidate the baseline.

## Current limitations

Two identical diagnostics produced by the same rule on identical
source lines in one file may share a fingerprint.

Moving code to another file changes its fingerprint.

Major changes to the diagnosed source line can cause the diagnostic to
be treated as new.
