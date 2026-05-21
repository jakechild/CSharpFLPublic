# CSharpFL


CSharpFL is a sample C# solution that demonstrates automated spectrum-based fault localization (SBFL). The `SBFLApp` console utility instruments the unit tests, executes them, collects coverage data, and ranks potentially faulty statements with several well-known SBFL metrics.

 
[![Release Version](https://img.shields.io/github/v/release/JakeChild/CSharpFLPublic?style=flat-square)](https://github.com/JakeChild/CSharpFLPublic/releases/latest)

[![GitHub license](https://img.shields.io/github/license/JakeChild/CSharpFLPublic?style=flat-square)](LICENSE)
![GitHub Repo size](https://img.shields.io/github/repo-size/JakeChild/CSharpFLPublic?style=flat-square&color=3cb371)
[![GitHub Repo Languages](https://img.shields.io/github/languages/top/JakeChild/CSharpFLPublic?style=flat-square)](https://github.com/JakeChild/CSharpFLPublic/search?l=c%23)
[![NET 8.0](https://img.shields.io/badge/dotnet-8.0-purple.svg?style=flat-square&color=512bd4)](https://learn.microsoft.com/zh-cn/dotnet/core/whats-new/dotnet-8)
[![C# 11](https://img.shields.io/badge/c%23-11-brightgreen.svg?style=flat-square&color=6da86a)](https://learn.microsoft.com/zh-cn/dotnet/csharp/whats-new/csharp-11)


[![Build, Test, and Versionize](https://github.com/jakechild/CSharpFLPublic/actions/workflows/versionize-on-master.yml/badge.svg)](https://github.com/jakechild/CSharpFLPublic/actions/workflows/versionize-on-master.yml)
[![Publish Release Assets](https://github.com/jakechild/CSharpFLPublic/actions/workflows/release-on-tag.yml/badge.svg)](https://github.com/jakechild/CSharpFLPublic/actions/workflows/release-on-tag.yml)



## Repository layout
- **MathApp/** – Minimal console application used as the subject under test. `MathOperations.cs` contains the arithmetic routines exercised by the tests, and `SeriesOperations.cs` provides richer sequence and statistics helpers for multi-file experimentation.
- **MathApp.Tests/** – xUnit test project with addition, subtraction, and series-analysis scenarios. Some subtraction tests intentionally fail so that the fault-localization workflow has both passing and failing executions to analyze.
- **SBFLApp/** – Console application that performs the SBFL workflow.
  - `Program.cs` discovers test methods, keeps instrumentation in sync, coordinates per-test execution, and manages coverage artifacts in a dedicated working folder.
  - `Spectrum.cs` and `LogStatementRewriter.cs` contain the Roslyn rewriters that add and clean up logging statements.
  - `Rank.cs` calculates suspiciousness scores using the Tarantula, Ochiai, D*, Op2, and Jaccard formulas and exports the values to CSV or Markdown.
- **MathApp.sln** – Solution file tying the application and test project together.

## Prerequisites
- [.NET SDK 8.0](https://dotnet.microsoft.com/download) or later.
- Windows, macOS, or Linux environment capable of running the .NET CLI.

## Restoring and building
```bash
cd CSharpFLPublic
dotnet restore
dotnet build
```

## Running the sample
`SBFLApp` accepts the solution directory, the test project name, the project under test, and optional boolean flags that control whether instrumentation should be reset and whether detailed test output is displayed. It instruments the discovered tests in place, executes them, aggregates the coverage data, and ranks statements by suspiciousness. Run the utility from the repository root:

```bash
dotnet run --project SBFLApp/SBFLApp.csproj . MathApp.Tests MathApp
```

Key behaviors:
- Test files are rewritten on disk during instrumentation. The tool removes previous instrumentation when rerun and updates the injected logging if necessary.
- Coverage GUIDs are written to `<FullyQualifiedTestName>.coverage` files inside a `Coverage/` directory created next to the running application. Each test first logs to `__sbfl_current_test.coverage.tmp` and the file is promoted when the run finishes so stale data is never mixed with new results.
- Once the run finishes, a `suspiciousness_report.csv` file is written to the provided solution directory. Use the optional flags below to tailor the output:

  - `--top <count>` (or `-t`) limits the report to the highest-suspicion statements.
  - `--report-format <csv|markdown>` switches between CSV and Markdown export formats.
  - `--report-path <file>` writes the report to a custom location.
  - `--summary` (or `-s`) prints the top results to the console (defaults to 10 when `--top` is omitted).
  - `--test <fullyQualifiedTestName>` runs a single test instead of the full test suite.

To discard instrumentation and delete generated coverage artifacts, provide the optional reset flag:

```bash
dotnet run --project SBFLApp/SBFLApp.csproj . MathApp.Tests MathApp --reset
```

This cleans modified test sources, removes coverage files, and clears cached GUID mappings. Use `--verbose` (or pass `true` for the final argument) to stream the test output for each execution when additional verbosity is desired.

To execute a specific test, pass the fully qualified test name:

```bash
dotnet run --project SBFLApp/SBFLApp.csproj . MathApp.Tests MathApp --test MathApp.Tests.MathTests.Subtract_WithPositiveNumbers_ReturnsCorrectDifference
```

## Running the test suite
The repository includes traditional unit tests as well as instrumentation hooks. Execute all tests with:

```bash
dotnet test
```

The subtraction scenarios are expected to fail—they provide failing executions for the SBFL ranking.
