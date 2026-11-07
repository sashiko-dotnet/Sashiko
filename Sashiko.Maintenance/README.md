# 🌸 Sashiko Maintenance Tool

The **Sashiko Maintenance Tool** is a small command‑line utility used to
maintain and regenerate embedded data for the Sashiko ecosystem.  
It provides a clean, extensible command dispatcher and a set of library‑specific
maintenance commands.

This tool is intended for contributors working on Sashiko’s internal data
registries (languages, names, etc.). It is not required at runtime by
consumers of the Sashiko libraries.

---

## 🚀 Running the Tool

From the root of the repository:

```bash
dotnet run --project Sashiko.Maintenance -- <category> <command>
```

Example:

```bash
dotnet run --project Sashiko.Maintenance -- languages update
```

---

## 🧩 Command Structure

The tool uses a simple two‑level command structure:

```text
<category> <command>
```

### Available Categories

| Category | Description |
|----------|-------------|
| `geography` | Validates and polishes embedded world, country, region and city collections |
| `languages` | Maintains the embedded language registry |
| `names` | Maintains the embedded name registries |

#### `languages` Commands
| Command | Description |
|---------|-------------|
| `update` | Downloads and regenerates the embedded `languages.json` file |

#### `names` Commands
| Command | Description |
|---------|-------------|
| `polish` | Sorts and deduplicates every embedded `names.json` file |

Example:

```bash
dotnet run --project Sashiko.Maintenance -- languages update
```

This command fetches the ISO 639‑3 registry, parses it, maps it to the
Sashiko language model, and writes the updated JSON file into the
`Sashiko.Languages` project.

Example:

```bash
dotnet run --project Sashiko.Maintenance -- names polish
```

This command reads every `names.json` file under the `Sashiko.Names/Data`
folder, trims values, removes blank or duplicate entries, sorts each array
alphabetically, and writes the polished data back as clean indented JSON.

---

#### `geography` Commands

| Command | Description |
|---------|-------------|
| `polish` | Validates the entire proposed dataset, normalizes text, and sorts JSON collections |
| `validate` | Checks canonical values, sources, references and partition ownership without writing |
| `check` | Fails if polishing would change a file; used by CI |
| `validate-release` | Also requires recorded source reuse review before Geography publication |

Geography runtime loading trusts the data checked here and in tests. These
commands use Sashiko's validated registry loaders and schema inspection, followed
by domain checks. They do not contact external registries. Polishing never
silently removes duplicates and validates every collection before writing.

```bash
dotnet run --project Sashiko.Maintenance -- geography polish
dotnet run --project Sashiko.Maintenance -- geography check
```

The release workflow checks canonical data and source review before testing and
packing Geography. It does not rewrite the dataset after tests.

## ✨ Architecture Overview
The maintenance tool is built around three core concepts:

### 1. Command Dispatcher
A central router that maps categories to handlers:

```csharp
internal static readonly Dictionary<string, ICommandHandler> Handlers;
```

This makes the tool easy to extend — simply register a new handler.

### 2. Command Handlers
Each category has its own handler implementing:

```csharp
public interface ICommandHandler
{
    Task DispatchAsync(string? command);
}
```

Handlers are instance‑based and support test hooks for full isolation.

### 3. Library‑Specific Maintenance Logic
Each library exposes a focused maintenance entry point:

```csharp
LanguageRegistryMaintenance.UpdateEmbeddedLanguagesAsync();
NamePoolMaintenance.PolishEmbeddedNamesAsync();
```

This keeps the dispatcher clean and the responsibilities well‑separated.

---

## 🧪 Testing
The maintenance tool is fully covered by a lightweight but meaningful test suite:

- Command routing
- Handler invocation
- Help and error output
- Deterministic update behavior
- File generation logic
- Name pool polishing behavior
- Project root discovery

Console output is captured safely using `Console.SetOut` with proper restoration
to avoid global state pollution.

Parallel test execution is disabled for command‑related tests to ensure
deterministic behavior.

---

## ➕ Extending the Tool
To add a new category:

1. Create a new handler implementing `ICommandHandler`

2. Register it in `CommandDispatcher.Handlers`

3. Add tests for routing and handler behavior

4. Add documentation to this README

Example:

```csharp
Handlers["cultures"] = new CultureCommandHandler();
```

---

## 🤝 Contributing

Contributions are welcome.  
Please see [CONTRIBUTING.md](../CONTRIBUTING.md) in the repository root.

---

## 📄 License
This project is licensed under the **Apache License 2.0**.  
See the [LICENSE](../LICENSE) file for the full license text.

Copyright © 2026 Alexandru Luca (alex98luca)
