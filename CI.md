# Continuous integration

The GitHub Actions workflow at `.github/workflows/ci.yml` runs on pushes and pull requests with
read-only repository permissions. It restores the .NET 8 solution, verifies formatting, builds
with warnings-as-errors and analyzers, runs every test project, audits direct and transitive NuGet
dependencies for known vulnerabilities, and uploads TRX and audit output.

## Local equivalents

Run these commands from the repository root:

```powershell
dotnet restore Pricing.sln --nologo
dotnet format Pricing.sln --verify-no-changes --no-restore --verbosity diagnostic
dotnet build Pricing.sln --configuration Release --no-restore --nologo
dotnet test Pricing.sln --configuration Release --no-build --no-restore --nologo --logger trx --results-directory TestResults
dotnet list Pricing.sln package --vulnerable --include-transitive
```

The build inherits nullable reference types, warnings-as-errors, .NET analyzers, and code-style
enforcement from `Directory.Build.props`. Project-reference direction is enforced by
`Directory.Build.targets` during the build.

This development machine has only the .NET 10 runtime installed. To execute the `net8.0` tests
locally here, set `$env:DOTNET_ROLL_FORWARD='Major'` before `dotnet test`. CI installs and uses the
.NET 8 SDK/runtime directly and does not use runtime roll-forward.
