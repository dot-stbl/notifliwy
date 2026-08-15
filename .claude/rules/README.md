# C# Development Rules - Notifliwy Project

Quick reference for all C# development tasks in this project.

## Rules Index

| File | Description |
|------|-------------|
| [naming.md](naming.md) | Naming conventions: interfaces, classes, lambda parameters |
| [patterns.md](patterns.md) | Code patterns: primary constructors, pattern matching, records |
| [xml-docs.md](xml-docs.md) | XML documentation rules |
| [logging.md](logging.md) | Structured logging patterns |
| [result.md](result.md) | Result<T> pattern for error handling |

## Key Principles

- **Primary constructors** — Use C# 12 primary constructor syntax
- **No private methods** — Use file static classes with extension methods or local functions
- **Async throughout** — All I/O operations use async/await, suffix `Async`
- **Structured logging** — Always use named parameters
- **Result pattern** — For expected failures, not exceptions