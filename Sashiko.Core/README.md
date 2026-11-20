# 🌸 Sashiko.Core

**Sashiko.Core** is the foundational utility package of the Sashiko ecosystem.

It provides small, reusable building blocks used by higher-level Sashiko packages: conversions, JSON file helpers, text normalization, probability helpers, and shared primitives.

---

## ✨ Features

- Strongly typed memory conversions
- Strongly typed bandwidth conversions
- JSON file read/write helpers
- Staged byte/stream file writes and bounded reads
- Bounded GZip/Brotli payload compression with explicit effort settings
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

### Byte-size formatting

```csharp
using System.Globalization;
using Sashiko.Core.Conversions;
using Sashiko.Core.Models.Enums;

var size = ByteSize.Format(1_572_864, formatProvider: CultureInfo.InvariantCulture);
// 1.5 MiB
var localizedSize = ByteSize.Format(1_500_000, ByteUnitStyle.Decimal,
    formatProvider: CultureInfo.GetCultureInfo("it-IT"));
// 1,5 MB
```

`ByteSize.Format` takes a signed `long` byte count. It defaults to binary units
(`B`, `KiB`, `MiB` through `EiB`). Use `ByteUnitStyle.Decimal` for powers of
1000 (`B`, `kB`, `MB` through `EB`). The number uses the supplied format provider
or the current culture. It shows up to two fractional digits by default, rounds
midpoints away from zero, and promotes to the next unit if rounding reaches its
threshold. Use the original byte count for storage, limits, and comparisons.

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

### Payload compression

`Sashiko.Core.Compression.CompressedData` offers `Compress`, `CompressAsync`,
`Decompress` and `DecompressAsync`. The caller explicitly chooses GZip or Brotli
and independent input/output byte limits. The methods return complete byte arrays
and leave the source stream open on success, failure and cancellation.

```csharp
using System.Text;
using Sashiko.Core.Compression;

var payload = Encoding.UTF8.GetBytes("{\"revision\":1}");
using var input = new MemoryStream(payload, writable: false);
var compressed = await CompressedData.CompressAsync(
    input, CompressionFormat.Brotli,
    maxInputBytes: 16 * 1024 * 1024,
    maxOutputBytes: 16 * 1024 * 1024,
    options: new CompressionOptions { BrotliQuality = 4 });

using var encoded = new MemoryStream(compressed, writable: false);
var restored = await CompressedData.DecompressAsync(
    encoded, CompressionFormat.Brotli,
    maxInputBytes: 16 * 1024 * 1024,
    maxOutputBytes: 16 * 1024 * 1024);
```

- Null options use .NET's `CompressionLevel.Optimal`. Set `Level` to choose a
  general preference, such as `Fastest` or `SmallestSize`, for either codec.
  Alternatively, set `BrotliQuality` from 0 to 11 for Brotli. The two settings
  cannot be combined, and quality is rejected for GZip. `NoCompression` still
  produces the selected wire format. Format/effort choices are never switched
  automatically. Record the format with application data; decoding needs the
  format but not the original effort setting.
- Limits are inclusive, from zero through `Array.MaxLength`. For compression
  they bound original input and encoded output, including finalization bytes;
  for decompression they bound encoded input and decoded output. Zero allows
  empty content, but its valid encoded representation still needs output space.
  Empty encoded input is rejected. GZip empty encoding uses a standard 20-byte
  empty member because .NET's encoder otherwise emits no member for empty input.
- Input is buffered using `BoundedRead`, from the current source position to EOF,
  before codec work. No seeking or reported length is needed; input can consume
  one extra byte to detect overflow. The source is never disposed or rewound.
  Even data ignored by a decoder is consumed as part of the bounded input. Frame
  extraction from a larger protocol is not provided.
- These are **bounded payload APIs**, not constant-memory streaming transforms.
  Input, output accumulation, returned arrays and codec workspace can coexist.
  The byte limits are not total-memory, native-allocation or CPU-time quotas;
  large limits or expensive quality settings can still exhaust resources.
- Async operations observe cancellation during source/decoder reads, between
  encoder writes and after finalization before returning. Pending reads depend
  on the source's token support; codec CPU work and tokenless finalization may
  finish before cancellation is observed. No partial output array is returned.
  Async methods do not guarantee background execution of codec CPU work; the
  caller controls scheduling for expensive workloads.
- Invalid arguments fail before input reads. Bounds and codec-reported invalid
  data throw `InvalidDataException`; Brotli decoder `InvalidOperationException`
  is normalized with its cause preserved. Source-read exceptions propagate.
- **A successful decode does not establish complete format validity or integrity.**
  .NET can accept truncated data and return partial or even complete content.
  It may ignore trailing bytes. GZip can decode concatenated members; Brotli
  reads the first stream. These follow runtime decoder semantics, which can
  change with runtime versions/configuration. Use a trusted decoded length/hash
  or an authenticated container when integrity matters. This is separate from
  strict validation of all compressed framing bytes.

Decoded bytes are preserved exactly. Encoded bytes are not promised to be
canonical or stable across runtime versions, effort settings or write chunking.

#### Verified decompression

Use `DecompressVerified` or `DecompressVerifiedAsync` when you have an exact
decoded byte count and a trusted SHA-256 digest for the original content. Supply
independent limits for encoded input and decoded output.

```csharp
var verification = new DecompressionVerification(
    storedLength, storedDecodedDigest, storedEncodedDigest);
var restored = await CompressedData.DecompressVerifiedAsync(
    encoded, CompressionFormat.GZip,
    maxInputBytes: 17 * 1024 * 1024,
    maxOutputBytes: 16 * 1024 * 1024,
    verification: verification,
    cancellationToken: cancellationToken);
```

- The declared length must fit within the caller's output limit. Decoding stops
  if it exceeds that declared length. An undersized result or a digest mismatch
  throws `InvalidDataException` without returning any bytes.
- Pass SHA-256 digests as 32 raw bytes. The optional encoded digest covers all
  bytes from the current source position to EOF, including any bytes the codec
  would ignore. It is verified before decoding. `DecompressionVerification`
  copies both digests on construction and can be reused across calls.
- These methods follow the same bounds and stream ownership rules as ordinary
  decompression. Hashing runs synchronously, and the async method observes
  cancellation between stages and before returning a verified result.
- Verification depends on the source of the expected digests. Use authenticated
  metadata when a sender could replace both a payload and its digests. The
  codec's framing behavior remains the same as for ordinary decompression.

The implementation uses .NET's codec APIs and the existing bounded reader. It
adds no external package or codec source/binary. Sashiko's integration remains
Apache-2.0; .NET and codec components retain their existing permissive licenses
and applicable notices. The
[design and license record](https://github.com/sashiko-dotnet/Sashiko/blob/master/docs/design/bounded-compression.md)
documents alternatives, format behavior and distribution responsibilities.
Filesystem publication and encryption remain caller composition decisions.

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
- codec interoperability, input/output limits, effort settings and stream ownership

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
