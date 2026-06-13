# C# Exercises

LeetCode exercises in C#.

## Progress

<!-- PROGRESS-START - DO NOT EDIT -->
| Solved |
| ------ |
| 0 |

**Last solved:** -
<!-- PROGRESS-END -->

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Setup

Restore tools and install git hooks (run once after cloning):

```sh
dotnet tool restore
dotnet husky install
```

## Running Tests

All tests:

```sh
dotnet test
```

Single problem (filter by class name):

```sh
dotnet test --filter TwoSumTests
```

## Pre-commit Hooks

On every commit, Husky automatically runs:

1. `dotnet format --verify-no-changes` — fails if any file is not formatted
2. `dotnet build -warnaserror` — fails if there are compiler warnings or errors

To fix formatting before committing:

```sh
dotnet format ExerciseCSharp.slnx
```
