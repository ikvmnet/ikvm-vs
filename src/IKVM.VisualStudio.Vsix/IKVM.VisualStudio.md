# IKVM Extension for Visual Studio

[IKVM.NET](https://github.com/ikvmnet/ikvm) is an implementation of the Java Virtual Machine built on .NET. This extension brings first-class Java project support to Visual Studio, allowing you to write, build, and debug Java code that targets the .NET runtime.

## Features

- **Java project type** — open and create `.ikvmproj` projects backed by the [IKVM.NET SDK](https://github.com/ikvmnet/ikvm)
- **Java syntax highlighting** — TextMate grammar for `.java` source files
- **Solution Explorer integration** — native support for `.java`, `.jar`, and `.class` file items
- **MSBuild / SDK-style projects** — full compatibility with the IKVM.NET MSBuild SDK and standard Visual Studio build tooling

## Requirements

- Visual Studio 2026 or later
- [IKVM.NET SDK](https://www.nuget.org/packages/IKVM.NET.Sdk) (referenced from your project file)

## Getting Started

1. Install the extension from the Visual Studio Marketplace.
2. Create a new `.ikvmproj` project or open an existing one.
3. Add `.java` source files and build — the IKVM compiler will produce a .NET assembly from your Java code.

For documentation and source code, visit [github.com/ikvmnet/ikvm-vs](https://github.com/ikvmnet/ikvm-vs).
