# 🌸 Contributing to Sashiko

Thank you for your interest in contributing to Sashiko.

Sashiko is an ecosystem of small, carefully maintained .NET packages. Each package should feel useful on its own, while still sharing the same standards, tone, and engineering care as the rest of the repository.

The goal is simple: keep Sashiko friendly to contribute to, pleasant to use, and clean enough that future work does not become future debt.

---

## 💬 Start With a Conversation

For non-trivial changes, please open an issue or start a discussion before writing a large patch.

Good topics include:

- new package proposals
- public API changes
- embedded data updates
- release automation
- performance improvements
- documentation improvements

Small fixes, typo corrections, and focused test improvements can go straight to a pull request.

---

## 🌿 Branching

Create focused branches from `master`.

Recommended branch names:

- `feature/package-name-short-description`
- `fix/package-name-short-description`
- `docs/short-description`
- `chore/short-description`

Examples:

```bash
git switch master
git switch -c docs/repository-polish
git switch -c feature/geography-country-registry
```

---

## 🧾 Commit Style

Use concise, scoped commit messages.

Examples:

```text
feature(names): add French name data
fix(languages): handle missing ISO aliases
docs(names): add release documentation
chore(names): prepare package metadata
```

Prefer small commits that tell a clear story. Avoid mixing generated data, package metadata, documentation, and behavior changes unless they are part of one inseparable change.

---

## 🛠️ Development Workflow

Before opening a pull request:

1. Build the affected projects.
2. Run the affected test projects.
3. Update README or changelog files when public behavior changes.
4. Keep unrelated formatting and metadata churn out of the diff.

Useful commands:

```bash
dotnet build Sashiko.slnx
dotnet test Sashiko.Names.Tests/Sashiko.Names.Tests.csproj
dotnet test Sashiko.Languages.Tests/Sashiko.Languages.Tests.csproj
```

For package release preparation, also verify packing:

```bash
dotnet pack Sashiko.Names/Sashiko.Names.csproj -c Release
```

---

## 🧠 Code Guidelines

Sashiko code should be:

- simple and explicit
- strongly typed
- small in scope
- easy to test
- consistent with existing package patterns

Prefer existing abstractions and helpers before adding new ones. Add comments only where they clarify non-obvious behavior.

---

## 🧪 Tests

Code changes should include tests when they affect behavior.

Data packages should also include validation tests for:

- schema shape
- embedded resource availability
- sorted and duplicate-free data
- package-specific quality guarantees

The test suite is part of the public trust story of the project.

---

## 📚 Embedded Data Contributions

Some Sashiko packages ship curated embedded data.

When contributing data:

- use public civil-registration, statistical, official, or culturally reviewed sources where possible
- document the source in the relevant package documentation
- keep data sorted and duplicate-free
- run the maintenance tool when one exists
- avoid depending on third-party fake-data libraries as source material

For `Sashiko.Names`, name data must remain Latin-readable unless a future
package explicitly supports another script as a first-class feature.

---

## 🔄 Maintenance Tooling

Internal data maintenance lives in `Sashiko.Maintenance`.

Examples:

```bash
dotnet run --project Sashiko.Maintenance -- languages update
dotnet run --project Sashiko.Maintenance -- names polish
```

Maintenance tooling is not required at runtime by consumer applications.

---

## 🎨 Assets and Branding

The source code is open source, but the Sashiko logo and branding assets in `/assets` are protected and are not part of the Apache 2.0 source-code license.

Use the current canonical asset paths when updating package metadata or documentation:

- `assets/sashiko-logo/github/sashiko-github-logo.png`
- `assets/sashiko-logo/nuget/sashiko-logo-nuget.png`

Do not modify, redistribute, or use the Sashiko branding for another project, product, or service without written permission from the maintainer. For the full asset terms, see [assets/ASSETS-LICENSE.md](./assets/ASSETS-LICENSE.md).

---

## 🤝 Conduct

Be respectful, constructive, and kind.

Sashiko is intended to be a serious engineering project with a welcoming community around it.

---

## 📬 Contact

For questions, suggestions, or professional inquiries:

- [sashiko@alex98luca.com](mailto:sashiko@alex98luca.com)
- Alexandru Luca (alex98luca)
