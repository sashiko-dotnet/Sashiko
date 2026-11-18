# Bounded payload compression

Status: implemented for review, 18 November 2026. Public APIs remain subject to review.

## Need and scope

Applications storing JSON documents, exports and snapshots need a choice of standard
codec with explicit bounds, effort settings, ownership and error handling. This
slice returns complete byte arrays. Input is buffered before encoding/decoding;
it is not a general streaming transform for arbitrarily large files.

## Build or adopt

| Option | Fit and gap |
| --- | --- |
| .NET GZipStream/BrotliStream directly | Proven codecs, existing runtime dependency and standard wire formats. Callers still repeat input/output limits, lifecycle and effort validation. |
| BoundedRead over a decoder | Already handles decoded-output limits. It does not independently limit encoded input, finish bounded encoding, or provide a shared codec/effort contract. |
| Additional codec/archive library | Not needed for this payload scope. Revisit if strict frame validation or archive extraction becomes a requirement; assess that exact dependency then. |

Use .NET's codecs and existing BoundedRead. Add CompressedData, an explicit
CompressionFormat, effort options, and an internal bounded encoding buffer in
Sashiko.Core.Compression. No external NuGet package, copied codec implementation,
native adapter, cloud service, subscription or network operation is introduced.
The cohesive BCL-only module fits Core. Applications retain format metadata,
encryption, hashes, file publication, retention and directory policy.

The reusable value is independent bounds, complete results, lifecycle handling,
settings validation, interoperability and documented runtime behavior. Both
formats share this policy and have format-specific tests.

## Licensing and independence

Sashiko's new integration code remains Apache-2.0. It calls the installed .NET
runtime instead of bundling codec sources/binaries in the Core package. The
[.NET 10 license](https://github.com/dotnet/runtime/blob/v10.0.0/LICENSE.TXT)
is MIT. The runtime's
[third-party notices](https://github.com/dotnet/runtime/blob/v10.0.0/THIRD-PARTY-NOTICES.TXT)
include the permissive zlib-ng and Brotli terms; the upstream
[Brotli license](https://github.com/google/brotli/blob/master/LICENSE) is MIT.
These permit use and redistribution under their stated conditions. Preserve
applicable notices when distributing third-party material, including a bundled
runtime. Additional dependencies or vendored implementations require a new review.
This records the checked licensing basis, not a guarantee against all legal claims.

## Contract decisions

- Format is mandatory. No automatic selection/detection or format switching occurs.
- Null effort options select runtime Optimal. Level and BrotliQuality are mutually
  exclusive; explicit quality is Brotli-only, from 0 to 11. NoCompression still
  produces an encoded stream, not raw passthrough. Effort is not needed to decode.
- Limits are independent inclusive integers, zero through Array.MaxLength. Input
  consumption uses BoundedRead, including its one-byte oversize probe. Encoded
  output is bounded during writes and codec finalization. Decoded output uses
  BoundedRead over the selected decoder.
- Sources remain open, advance from their current position and are not rewound.
  The entire bounded input is consumed before codec work, including bytes a decoder
  might ignore. These APIs do not read one frame out of an enclosing protocol.
- Results appear only on success. Multiple input/output buffers and codec workspace
  can coexist. Byte limits are not total allocation/native-memory or CPU-time quotas.
- Async APIs observe cancellation around reads, between encoder writes and after
  encoder finalization. In-progress codec CPU work and tokenless disposal are not
  guaranteed to stop immediately.
  Async I/O does not guarantee background scheduling of codec CPU work.
- Limits, empty encoded input and codec-reported invalid data use InvalidDataException.
  Brotli's InvalidOperationException from the owned decoder is normalized with its
  original exception preserved. Errors from caller source reads are not reclassified.

## Runtime edge cases

Probed on macOS arm64 with .NET 10.0.10. A stream read can end without proving that
the compressed frame was complete: both runtimes accepted tested truncations and
returned partial content. Missing GZip footer bytes could still yield full output.
Trailing bytes were accepted. GZip decoded concatenated members; Brotli returned
the first stream. Do not infer complete format validity from a successful decode.
These behaviors are runtime-dependent; Sashiko does not add a parser or change
process-wide AppContext switches. Use a trusted decoded length/hash or an
authenticated container when content integrity matters. Such checks do not prove
that an otherwise recoverable compressed frame has all its framing bytes.

.NET emits zero GZip bytes for empty input. Sashiko emits the standard 20-byte
empty GZip member (RFC 1952 header, empty DEFLATE block, zero CRC-32 and ISIZE) so
empty results remain interoperable. It is a format constant, not a new codec.
Python gzip and the local gzip command accepted that member. Brotli empty encoding
is supplied by BrotliStream. Encoded-output bounds include these empty encodings.

Strict framing, custom archive/container formats, automatic compression bypass,
additional codecs and streaming to caller-owned destinations are deferred.

## Validation and revisit criteria

Tests cover direct-codec interoperability, empty data, byte preservation, options,
non-seekable/short reads, exact/input/output limits, expansion, finalization,
source errors, ownership, cancellation, malformed data and runtime behavior parity.
Existing native Core CI includes the new tests on Windows, macOS and Linux,
preserving its existing job names. No timing thresholds are part of CI.
Revisit placement/dependencies if real consumers need larger-than-array streaming
or strict framing guarantees that cannot be obtained from the public runtime API.
