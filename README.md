# TempData and Session property-type validation

Validation sample for [dotnet/aspnetcore issue #69136](https://github.com/dotnet/aspnetcore/issues/69136), covering property types supplied by `[SupplyParameterFromTempData]` and `[SupplyParameterFromSession]` in a Blazor Web App using Static SSR.

## Validation status

| Configuration | Supported types | Unsupported types | Result |
| --- | --- | --- | --- |
| Debug Static SSR | Passed | Expected rejection passed | Completed |
| Published Release Static SSR | Passed | Expected rejection passed | Completed |
| Self-contained trimmed `win-x64` | Blocked before setter execution | Not run | Configuration limitation |

The trimmed publish generated output, but the .NET 11 RC1 toolchain reported that Razor Components and Session State middleware don't support trimming or Native AOT. The executable started, but `/` and `/supported/set` returned HTTP 500 during Razor component activation before any validation property was assigned, persisted, or deserialized. This is reported as a deployment-configuration limitation, not a TempData or Session serialization regression.

## Environment

- .NET SDK: `11.0.100-rc.1.26425.128`
- ASP.NET Core shared runtime: `11.0.0-rc.1.26425.128`
- Target framework: `net11.0`
- Application: Blazor Web App, Static SSR, no authentication or interactive render mode
- Session store: distributed memory cache
- Validated platform: Windows, with `win-x64` used for the trimmed publish

The required SDK is pinned by `global.json`. Install that exact SDK before building the sample.

## Setup

From the repository root:

```powershell
dotnet --info
dotnet restore .\TempDataSessionPropertyTypes.slnx
dotnet build .\TempDataSessionPropertyTypes.slnx
dotnet run --project .\src\TempDataSessionValidation\TempDataSessionValidation.csproj
```

The default launch profiles use:

- `http://localhost:5013`
- `https://localhost:7213`

If the HTTPS development certificate isn't trusted, run:

```powershell
dotnet dev-certs https --trust
```

The application registers Razor Components, a distributed memory cache, and Session services. Session middleware runs before Razor component endpoint mapping.

## Manual validation

### Supported types

1. Open `/supported/set` and wait for the response to complete.
2. Follow the **Read** link. It disables enhanced navigation so the read occurs in a new HTTP request.
3. Verify that every non-null TempData and Session result is `Pass` and that runtime types match the declared property types.
4. Use **Full-request read again** only as an additional observation; TempData consumption behavior isn't the subject of issue #69136.

The supported table covers:

- `string`, `int`, and `bool`
- Int-backed enum
- `Guid` and `DateTime`
- Nullable value, explicit null, and missing value
- Populated, empty, and null `List<string>`
- `Dictionary<string, int>`
- `string[]`

### Unsupported types

Each unsupported attribute/type combination has an isolated route under `/unsupported`. This prevents an early rejection from hiding results for other cases.

The cases are:

- TempData and Session with a POCO
- TempData and Session with `List<POCO>`
- TempData and Session with a byte-backed enum

Expected behavior:

- Session rejects the property during subscription and identifies the property, component, and property type.
- TempData rejects the value later during persistence and identifies the unsupported type.
- No unsupported value is silently accepted or dropped.

## Published validation

Publish the ordinary Release application with:

```powershell
dotnet publish .\src\TempDataSessionValidation\TempDataSessionValidation.csproj `
	-c Release `
	-o .\artifacts\published
```

Run the executable from the publish directory and repeat the supported and isolated unsupported routes. The checked-in evidence records the completed published validation; generated publish output itself is intentionally excluded from source control.

## Trimming exploration

The tested command was:

```powershell
dotnet publish .\src\TempDataSessionValidation\TempDataSessionValidation.csproj `
	-c Release `
	-r win-x64 `
	--self-contained true `
	-p:PublishTrimmed=true `
	-p:SuppressTrimAnalysisWarnings=false `
	-p:TrimmerSingleWarn=false `
	-o .\artifacts\trimmed-win-x64
```

The publish completed with trim-analysis warnings, including explicit warnings that Razor Components and Session State middleware don't support trimming or Native AOT. At runtime, component activation failed with an `InvalidOperationException` for the `NotFound` component before `/supported/set` executed.

The folders `evidence/trimmed/Supported Type` and `evidence/trimmed/unsupported` are intentionally empty because neither validation path executed. Successful Debug or ordinary published screenshots must not be placed there.

## Evidence

The `evidence` directory contains the captured environment, commands, build output, screenshots, browser results, terminal diagnostics, and trimmed-runtime summary.

Some captured logs contain absolute local filesystem paths from the validation machine. They are retained verbatim as command provenance and contain no credentials. Generated directories such as `bin`, `obj`, `.vs`, and `artifacts` are excluded.

## Documentation observation

The referenced ASP.NET Core documentation explains TempData and Session persistence and setup, but a complete supported property-type list for `[SupplyParameterFromTempData]` and `[SupplyParameterFromSession]` wasn't found. In particular, it doesn't clearly enumerate all tested scalar, enum, nullable, collection, dictionary, and array cases or the isolated unsupported cases. The result tables and evidence in this repository record the behavior observed with .NET 11 RC1.

## Scope

Published output and trimming were the additional selected axes for this validation. An existing .NET 10 application upgrade, multi-instance or proxy deployment, Hot Reload, an IDE-versus-CLI comparison, and container deployment weren't selected and aren't claimed by this repository.

## Outcome

Debug and ordinary published Release validation passed for supported property types and expected unsupported-type rejection. The trimmed comparison remains blocked by the unsupported deployment configuration and must not be reported as a successful supported-type run.