# IKVM Extension for Visual Studio

[IKVM.NET](https://github.com/ikvmnet/ikvm) is an implementation of the Java Virtual Machine built on .NET. This extension brings first-class Java project support to Visual Studio, allowing you to write, build, and debug Java code that targets the .NET runtime.

## Features

- **Java project type** — open `.ikvmproj` projects backed by the [IKVM.NET SDK](https://github.com/ikvmnet/ikvm); Java source is compiled with `javac` and translated by `ikvmc` into an ordinary .NET assembly
- **Solution Explorer integration** — `.java` source files appear as project items, with dependencies and references
- **MSBuild / SDK-style projects** — full compatibility with the IKVM.NET MSBuild SDK and standard Visual Studio build tooling

## Requirements

- Visual Studio 2022 17.14 or later
- [IKVM.NET SDK](https://www.nuget.org/packages/IKVM.NET.Sdk) (referenced from your project file)

## Getting Started

1. Install the extension from the Visual Studio Marketplace.
2. Open an existing `.ikvmproj` project, or create one by hand (see the [README](https://github.com/ikvmnet/ikvm-vs#readme)).
3. Add `.java` source files and build — the IKVM compiler will produce a .NET assembly from your Java code.

For documentation and source code, visit [github.com/ikvmnet/ikvm-vs](https://github.com/ikvmnet/ikvm-vs).
