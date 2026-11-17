# 🌸 Sashiko.Core

**Sashiko.Core** is the foundational utility package of the Sashiko ecosystem.

It provides small, reusable building blocks used by higher-level Sashiko packages: conversions, JSON file helpers, text normalization, probability helpers, and shared primitives.

---

## ✨ Features

- Strongly typed memory conversions
- Strongly typed bandwidth conversions
- JSON file read/write helpers
- Staged byte/stream file writes and bounded reads
- Text normalization helpers
- Probability utilities and random selection helpers
- Cached environment/runtime information
- Zero external dependencies

---

## 📦 Installation

```bash
dotnet add package Sashiko.Core
```

---

## 🚀 Usage

### Memory conversions

```csharp
using Sashiko.Core.Conversions;
using Sashiko.Core.Models.Enums;

var gigabytes = MemoryConverter.Convert(
    1024,
    MemoryUnit.Megabytes,
    MemoryUnit.Gigabytes);

Console.WriteLine(gigabytes); // 1
```

### Bandwidth conversions

```csharp
using Sashiko.Core.Conversions;
using Sashiko.Core.Models.Enums;

var megabits = BandwidthConverter.Convert(
    1000,
    BandwidthUnit.Kilobits,
    BandwidthUnit.Megabits);

Console.WriteLine(megabits); // 1
```

### Runtime environment

```csharp
using Sashiko.Core.Environment;

var runtime = RuntimeInfo.Current;

Console.WriteLine(runtime.FamilyName);       // Windows, Linux, macOS, ...
Console.WriteLine(runtime.ArchitectureName); // x64, ARM64, ...
Console.WriteLine(RuntimeInfo.IsDebug);
```

### File persistence and bounded input

```csharp
using System.Text;
using Sashiko.Core.IO;

// The parent directory already exists and its access policy suits this payload.
var path = Path.Combine(appDataDirectory, "settings.json");
var bytes = Encoding.UTF8.GetBytes("{\"theme\":\"dark\"}");

await AtomicFile.WriteAsync(path, bytes, overwrite: true);
var saved = await BoundedRead.ReadAllBytesAsync(path, maxBytes: 1024 * 1024);
```

`AtomicFile` stages output beside its destination, flushes and closes it, then
publishes it with `File.Move`. Overwrite is opt-in. `BoundedRead` enforces an
inclusive limit on actual bytes consumed, including growing/non-seekable input.
Both offer synchronous, asynchronous and caller-owned stream overloads.

`Sashiko.Core.IO` is a namespace within `Sashiko.Core`; it requires no additional
package. Existing JSON helpers keep their current behaviour.

#### Atomic writes

`AtomicFile.Write` and `WriteAsync` accept bytes or a readable source stream.
The source is copied from its current position to EOF and remains open on
success, failure or cancellation. Memory supplied to `WriteAsync` must remain
unchanged until the operation completes.

- The parent directory must already exist. Output is staged in a uniquely named
  `.sashiko-<id>.tmp` file beside the destination, using `FileMode.CreateNew` and
  `FileShare.None`. The staged file is flushed with `Flush(flushToDisk: true)`
  and closed before publication.
- `overwrite` defaults to false; true permits create-or-replace. Use an
  application-controlled directory and coordinate writers. The runtime's
  no-overwrite move is not a portable atomic create-if-absent operation against
  competing processes: in .NET 10, a Unix destination-existence check can race a
  subsequent rename. No read/modify/write lock or version check is provided.
- On local filesystems supporting atomic same-directory rename, new readers see
  complete old or new content at publication. Already-open readers and sharing
  constraints follow OS rules. This guarantee does not extend to untested
  network filesystems, cloud-synced folders or unusual mount configurations.
  Flushing file contents does not flush the directory or guarantee survival
  after power loss. No multi-file transaction is provided.
- Read, write or flush failures before publication leave the destination
  untouched. Cleanup attempts only the temporary filename created by this
  operation; cleanup IO/access errors do not hide the original error. A process
  crash or failed cleanup can leave a temporary file. No automatic scavenging
  is performed. A commit error means publication was not confirmed; inspect the
  destination before retrying consequential work. There are no automatic retries.
- Observed directory or symbolic-link/reparse-point destinations are rejected
  before staging and again before the move. These checks are best-effort, not a
  path sandbox. Parent links follow ordinary OS resolution; hostile directory
  changes and file-identity races are outside this contract.
- **Replacement publishes the temporary file's access metadata.** Existing
  destination ACLs, permissions, ownership, attributes and timestamps are not
  copied. Unix staging requests owner read/write only, subject to umask; Windows
  staging inherits the parent directory's ACL. Use this API where that policy
  is appropriate for the published file, rather than where per-file ACLs or
  platform file encryption must be preserved. Sensitive payloads must already
  be encrypted if required; the API writes exactly the bytes supplied.
- Async writes check cancellation before staging, pass the token to writing or
  copying, and check it again before the synchronous flush/close/move section.
  Once that section begins, cancellation does not interrupt it or turn a
  successful commit into a canceled task. Pending source reads depend on the
  source stream's cancellation support.

Filesystem operations use the standard [.NET move semantics](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.move?view=net-10.0)
and [file flushing](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-10.0).
The concurrency limitation follows the [.NET 10 Unix implementation](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/IO/FileSystem.Unix.cs).

#### Verified stream writes

`AtomicFile.WriteVerified` and `WriteVerifiedAsync` add an exact byte-count and
SHA-256 check before publication. Use them when a completed stream might still
contain incomplete or unexpected content, such as a file being copied against
a trusted manifest. An ordinary `Write` copies to EOF without these checks;
`BoundedRead` enforces an upper bound rather than an exact size and digest.

```csharp
// Obtain both expectations from a trusted source, independently of this copy.
// expectedLength is a long; expectedSha256 contains the 32 digest bytes.
using var source = File.OpenRead(sourcePath);
await AtomicFile.WriteVerifiedAsync(
    destinationPath,
    source,
    expectedLength,
    expectedSha256,
    cancellationToken: cancellationToken);
```

- The source is read from its current position without inspecting its length or
  seeking. It remains open on every outcome, and its position is not restored.
  For a file copy, the caller owns the source file handle. The destination must
  not alias that source file; arbitrary stream/file identity is not detected.
- `expectedLength` must be nonnegative, including zero, and uses `long` so files
  need not fit in a managed array. Short input fails at EOF. One additional byte
  checks for oversized input, and is never written. Logical source consumption
  is limited to the expected count plus that probe; buffered sources may prefetch.
  The copy buffer is at most 80 KiB, reduced for smaller expectations.
- `expectedSha256` must contain exactly 32 bytes, not a hexadecimal string. Use
  `Convert.FromHexString` when needed. The digest is copied before source reads
  or the first await, so later mutations of the supplied buffer do not change
  the expectation. Invalid length/digest arguments fail before staging or reads.
- After copying, buffered output is flushed and the staged file is rewound and
  hashed through the same open handle using .NET's
  [`SHA256.HashData` / `HashDataAsync`](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.sha256).
  Only a matching staged length and digest permit the existing flush/close/move
  commit. Rereading costs an additional sequential pass over the staged file.
  This checks bytes returned by the filesystem, not physical-media durability.
- Length/hash mismatches throw `InvalidDataException`. IO errors and cancellation
  propagate; pre-publication failures preserve the destination and trigger the
  same operation-owned temporary-file cleanup as ordinary atomic writes.
- Async copying, flushing and hashing observe cancellation, with a final check
  immediately before the synchronous commit section. The overwrite, permissions,
  concurrency, link, cleanup and durability limits documented for atomic writes
  also apply here. In particular, no-overwrite is not an exclusive reservation
  against uncoordinated writers, and a commit error does not confirm publication.

This composes existing .NET copying, SHA-256 and the existing Sashiko staging
implementation. It introduces no codec, custom hash implementation, third-party
dependency or additional package. The API owns sequencing and failure handling;
applications still own manifests, directory layouts, locking and multi-file moves.
An expected hash must come from a trusted source: an attacker-replaceable hash
does not authenticate a file. No encryption or manifest authentication is added.

#### Bounded reads

`BoundedRead.ReadAllBytes` and `ReadAllBytesAsync` accept a path or a readable
stream and return the exact bytes from its current position to EOF.

- `maxBytes` is inclusive, may be zero, and must not exceed `Array.MaxLength`.
  The limit is enforced during consumption without inspecting `Length`,
  `Position` or seeking support.
- One extra byte distinguishes EOF from oversized input. Oversized input throws
  `InvalidDataException` without returning a partial payload. Caller-supplied
  streams remain open; their positions are not rolled back. At most
  `maxBytes + 1` bytes are consumed logically, although buffered sources may
  prefetch internally.
- The path overloads own and dispose their file handles and follow ordinary OS
  link behaviour. Async reads check cancellation between reads and after each
  read completes; interrupting a pending read depends on the underlying stream.
- The copy buffer is at most 80 KiB, reduced to `maxBytes + 1` for smaller limits.
  This follows the conventional [`Stream.CopyTo` default](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/IO/Stream.cs),
  rather than a workload-specific benchmark. The accumulation buffer and returned
  array can coexist, so the byte limit is not a total allocation, CPU-time or
  general decompression limit. Large limits can still exhaust memory.

---

## 🧪 Testing

The test suite covers:

- identity conversions
- cross-unit conversions
- round-trip conversions
- JSON file read/write behavior
- registry snapshot helpers
- probability helpers
- text normalization
- runtime environment metadata
- staged persistence failure/cleanup and bounded stream consumption
- exact-length and SHA-256 verification before publishing streamed output

IO coverage includes source failures, cancellation before commit, short and
non-seekable reads, growing files, exact/zero limits, cleanup ownership and
observed links. CI includes IO tests on Windows, macOS and Linux; Unix mode/link
tests are explicitly skipped on Windows. These tests do not establish power-loss
durability, forced disk-full/flush behaviour, hostile-path safety or network
filesystem guarantees.

Verified-write coverage includes multi-buffer/short/non-seekable reads, empty
payloads, wrong lengths and hashes, digest-buffer mutation, source failures,
cancellation, destination collisions, links and cleanup ownership.

---

## 🗺️ Roadmap

Future versions may include:

- temperature conversions
- time and duration helpers
- human-readable formatting helpers
- additional numeric abstractions

---

## 🤝 Contributing

Contributions are welcome.  
Please see [CONTRIBUTING.md](../CONTRIBUTING.md) in the repository root.

---

## 📄 License

This project is licensed under the **Apache License 2.0**.  
See [LICENSE](../LICENSE) for the full license text.

Copyright © 2026 Alexandru Luca (alex98luca)
