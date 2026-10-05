# IKVM Extension for Visual Studio

[![Build](https://github.com/ikvmnet/ikvm-vs/actions/workflows/IKVM.VisualStudio.yml/badge.svg)](https://github.com/ikvmnet/ikvm-vs/actions/workflows/IKVM.VisualStudio.yml)
[![Visual Studio Marketplace](https://img.shields.io/visual-studio-marketplace/v/IKVM.ikvm?label=Marketplace)](https://marketplace.visualstudio.com/items?itemName=IKVM.ikvm)

[IKVM.NET](https://github.com/ikvmnet/ikvm) is an implementation of the Java Virtual Machine built on .NET. This extension brings first-class Java project support to Visual Studio, letting you write, build, and manage Java code that compiles directly to .NET assemblies — without leaving the IDE.

---

## Features

### Java Project System
- **`.ikvmproj` project type** — a full SDK-style project backed by the [IKVM.NET SDK](https://github.com/ikvmnet/ikvm), integrated with Visual Studio's Common Project System (CPS). Java source is compiled with `javac` and translated by `ikvmc` into an ordinary .NET assembly.
- **Solution Explorer support** — `.java` source files appear as project items, with dependencies, references, and the IKVM project icon
- **MSBuild integration** — build, rebuild, and clean work through the standard Visual Studio build pipeline; CI builds work via `dotnet msbuild` with no extra tooling

### IKVM Dependencies
- **IKVM Dependencies node** — `IkvmReference` items (JARs and class folders converted to .NET assemblies) appear beside Dependencies, in a folder per target framework when a project has several, with their resolved assembly name and version in the Properties window
- **Manage IKVM Dependencies** — one place to add, remove, and edit references: from the Project menu, the Dependencies and IKVM Dependencies nodes, or **Ctrl+Alt+J**
  - choose the target frameworks each reference is used in, and view or edit each framework's settings; values that differ between frameworks are highlighted
  - ordered **Compile**, **Sources**, and **Depends on** lists, reordered by dragging
  - assembly name and version are detected through the project's IKVM package
  - changes are checked as you make them, and Save is disabled while anything is invalid, such as an empty Compile list or dependencies that form a cycle
  - references imported from other files, or written with MSBuild expressions, are shown read-only
- Works in any SDK-style project that references the IKVM package, including C# projects. Requires an IKVM package that declares the `IkvmReferences` project capability.

### Editor
- **Java syntax highlighting** — provided by Visual Studio's built-in Java TextMate grammar

### Marketplace & Distribution
- **Automatic install prompt** — projects using `IKVM.NET.Sdk` declare a `VsixDependency` on this extension, so Visual Studio prompts users to install it on first open
- **VSIX packaging** — single `.vsix` artifact published to the Visual Studio Marketplace on every tagged release

---

## Requirements

| Requirement | Version |
|---|---|
| Visual Studio | 2022 17.14 or later (amd64 or arm64) |
| .NET SDK | 9.0 or later (for building this repo) |
| [IKVM.NET SDK NuGet package](https://www.nuget.org/packages/IKVM.NET.Sdk) | referenced from your `.ikvmproj` |

---

## Getting Started

### Install the extension

Install from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=IKVM.ikvm), or search for **IKVM** in **Extensions → Manage Extensions** inside Visual Studio.

### Create a Java project

The extension does not yet ship project templates. Create a `.ikvmproj` file by hand:

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
    Commands/                    # Command IDs for IkvmPackage.vsct
    Images/                      # Project icon images (see tools/Generate-IkvmIcon.ps1) and vector icons
    Imaging/                     # Image monikers for the image manifest
    Packaging/                   # AsyncPackage registration
    ProjectSystem/               # CPS project type, capabilities, properties
      References/                # IKVM Dependencies node, commands, and reading and writing IkvmReference items
    UI/                          # Manage IKVM Dependencies dialog
  dist-vsix/                     # Packaging target that assembles the .vsix artifact
  dist-tests/                    # Test distribution target
.github/workflows/
  IKVM.VisualStudio.yml          # CI/CD: build, test, publish to Marketplace on tag
tools/
  Generate-IkvmIcon.ps1          # Regenerates the icon images
```

---

## Building from Source

```pwsh
# Restore packages
dotnet restore IKVM.VisualStudio.slnx

# Build the VSIX
dotnet msbuild /p:Configuration=Release IKVM.VisualStudio.dist.msbuildproj

# The output VSIX is written to dist/vsix/IKVM.vsix
```

To run the extension in the Visual Studio Experimental Instance, open `IKVM.VisualStudio.slnx` in Visual Studio and press **F5**.

---

## Contributing

Contributions are welcome. Please open an issue or pull request on [github.com/ikvmnet/ikvm-vs](https://github.com/ikvmnet/ikvm-vs).

This project follows the same contribution guidelines as [IKVM.NET](https://github.com/ikvmnet/ikvm).

---

## License

Licensed under the [MIT License](LICENSE).  
IKVM.NET is Copyright © Jerome Haltom and contributors.

Some icons are derived from [IntelliJ Platform](https://github.com/JetBrains/intellij-community) icons, licensed under the Apache License 2.0; see [THIRD-PARTY-NOTICES.txt](src/IKVM.VisualStudio.Vsix/THIRD-PARTY-NOTICES.txt).
