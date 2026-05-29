# IKVM Extension for Visual Studio

[![Build](https://github.com/ikvmnet/ikvm-vs/actions/workflows/IKVM.VisualStudio.yml/badge.svg)](https://github.com/ikvmnet/ikvm-vs/actions/workflows/IKVM.VisualStudio.yml)
[![Visual Studio Marketplace](https://img.shields.io/visual-studio-marketplace/v/IKVM.ikvm?label=Marketplace)](https://marketplace.visualstudio.com/items?itemName=IKVM.ikvm)

[IKVM.NET](https://github.com/ikvmnet/ikvm) is an implementation of the Java Virtual Machine built on .NET. This extension brings first-class Java project support to Visual Studio, letting you write, build, and manage Java code that compiles directly to .NET assemblies — without leaving the IDE.

---

## Features

### Java Project System
- **`.ikvmproj` project type** — a full SDK-style project backed by the [IKVM.NET SDK](https://github.com/ikvmnet/ikvm), integrated with Visual Studio's Common Project System (CPS)
- **Solution Explorer support** — `.java` source files, `.jar` archives, and `.class` files appear as first-class project items with dedicated icons
- **MSBuild integration** — build, rebuild, and clean work through the standard Visual Studio build pipeline; CI builds work via `dotnet msbuild` with no extra tooling

### Editor
- **Java syntax highlighting** — TextMate grammar for `.java` source files (sourced from VS Code's Java extension)
- **Language configuration** — bracket matching, comment toggling, and other editor affordances for Java source

### Marketplace & Distribution
- **Automatic install prompt** — projects using `IKVM.NET.Sdk` declare a `VsixDependency` on this extension, so Visual Studio prompts users to install it on first open
- **VSIX packaging** — single `.vsix` artifact published to the Visual Studio Marketplace on every tagged release

---

## Requirements

| Requirement | Version |
|---|---|
| Visual Studio | 2026 or later (amd64) |
| .NET SDK | 9.0 or later (for building this repo) |
| [IKVM.NET SDK NuGet package](https://www.nuget.org/packages/IKVM.NET.Sdk) | referenced from your `.ikvmproj` |

---

## Getting Started

### Install the extension

Install from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=IKVM.ikvm), or search for **IKVM** in **Extensions → Manage Extensions** inside Visual Studio.

### Create a new Java project

1. Open Visual Studio and choose **Create a new project**.
2. Search for **IKVM** or filter by language **Java**.
3. Select the **IKVM Java Class Library** (or application) template and follow the wizard.

### Add to an existing solution

Create a `.ikvmproj` file by hand or via `dotnet new`:

```xml
<Project Sdk="IKVM.NET.Sdk/8.x.x">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>
```

Add `.java` source files to the project directory — they are included automatically by default globs. Build with **Ctrl+Shift+B** or `dotnet build`.

---

## Repository Layout

```
src/
  IKVM.VisualStudio.Vsix/        # The VSIX extension (net472)
    Grammars/                    # TextMate grammar for .java files
    Icons/                       # File icon provider
    Images/                      # Image manifest and icons
    Packaging/                   # AsyncPackage registration
    ProjectSystem/               # CPS project type, capabilities, properties
  dist-vsix/                     # Packaging target that assembles the .vsix artifact
  dist-tests/                    # Test distribution target
.github/workflows/
  IKVM.VisualStudio.yml          # CI/CD: build, test, publish to Marketplace on tag
```

---

## Building from Source

```pwsh
# Restore packages
dotnet restore IKVM.VisualStudio.sln

# Build the VSIX
dotnet msbuild /p:Configuration=Release IKVM.VisualStudio.dist.msbuildproj

# The output VSIX is written to dist/vsix/IKVM.vsix
```

To run the extension in the Visual Studio Experimental Instance, open `IKVM.VisualStudio.sln` in Visual Studio and press **F5**.

---

## Contributing

Contributions are welcome. Please open an issue or pull request on [github.com/ikvmnet/ikvm-vs](https://github.com/ikvmnet/ikvm-vs).

This project follows the same contribution guidelines as [IKVM.NET](https://github.com/ikvmnet/ikvm).

---

## License

Licensed under the [MIT License](LICENSE).  
IKVM.NET is Copyright © Jerome Haltom and contributors.
