# ParameterNameAnalyzer

**ParameterNameAnalyzer** is a custom Roslyn Analyzer that enforces the use of named arguments for method parameters in C#. It ensures that all method calls include explicit parameter names, improving code readability and preventing confusion when working with multiple method overloads.

## Features

- Flags method calls that omit parameter names.
- Works with constructors, method overloads, and extension methods.
- Provides automatic code fixes to insert missing parameter names.

## Installation

You can install **ParameterNameAnalyzer** via NuGet:

```bash
dotnet add package ParameterNameAnalyzer
