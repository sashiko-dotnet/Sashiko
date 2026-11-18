using System.IO.Compression;
using Sashiko.Core.IO;

namespace Sashiko.Core.Compression
{
	/// <summary>Compresses and decompresses bounded byte payloads using .NET's GZip and Brotli codecs.</summary>
	/// <remarks>
	/// Reads the source from its current position to EOF and leaves it open on every outcome.
	/// Buffers input and output in memory; byte limits are not a total-memory or CPU-time quota.
	/// Decoder validation follows the runtime: some truncated input or trailing data can be accepted.
	/// Use a trusted decoded length/hash or authenticated container when integrity is required.
	/// See the Core README for format-specific behavior and ownership limits.
	/// </remarks>
	public static class CompressedData
	{
		private const int BufferSize = 80 * 1024;
		// RFC 1952 header, empty DEFLATE block, zero CRC-32 and zero ISIZE. .NET's
		// GZipStream emits no member for empty input, which external gzip tools reject.
		private static ReadOnlySpan<byte> EmptyGZip =>
			[0x1f, 0x8b, 0x08, 0, 0, 0, 0, 0, 0, 0xff, 0x03, 0, 0, 0, 0, 0, 0, 0, 0, 0];

		/// <summary>Returns a complete compressed payload, enforcing limits on original input and encoded output.</summary>
		/// <param name="source">Readable caller-owned stream. Its position is not restored.</param>
		/// <param name="format">Explicit output format; no automatic codec selection occurs.</param>
		/// <param name="maxInputBytes">Inclusive input limit, from zero through Array.MaxLength.</param>
		/// <param name="maxOutputBytes">Inclusive encoded-output limit, including codec headers and trailers.</param>
		/// <param name="options">Optional effort settings; defaults to the runtime's Optimal preference.</param>
		/// <exception cref="InvalidDataException">An input/output byte limit is exceeded.</exception>
		public static byte[] Compress(Stream source, CompressionFormat format, int maxInputBytes, int maxOutputBytes,
			CompressionOptions? options = null)
		{
			Validate(format, maxInputBytes, maxOutputBytes, options);
			var input = BoundedRead.ReadAllBytes(source, maxInputBytes);
			using var output = new LimitedBufferStream(maxOutputBytes);
			if (input.Length == 0 && format == CompressionFormat.GZip)
			{
				output.Write(EmptyGZip);
			}
			else
			{
				using var encoder = CreateEncoder(output, format, options);
				encoder.Write(input);
			}
			return output.ToArray();
		}

		/// <summary>Asynchronously returns complete compressed output, leaving the source open.</summary>
		/// <param name="source">Readable caller-owned stream, consumed from its current position.</param>
		/// <param name="format">Explicit output format.</param>
		/// <param name="maxInputBytes">Inclusive original-input limit, from zero through Array.MaxLength.</param>
		/// <param name="maxOutputBytes">Inclusive encoded-output limit, including codec finalization bytes.</param>
		/// <param name="options">Optional effort settings; defaults to the runtime's Optimal preference.</param>
		/// <param name="cancellationToken">Cancellation during reads/writes and before returning the result.</param>
		/// <exception cref="InvalidDataException">An input/output byte limit is exceeded.</exception>
		/// <exception cref="OperationCanceledException">Cancellation is observed before returning a result.</exception>
		/// <remarks>
		/// Limits have the same meaning as Compress. Cancellation is checked during input reads,
		/// between encoder writes, and after finalization before returning. A codec's in-progress
		/// CPU work or tokenless finalization may complete before cancellation is observed.
		/// No partial output array is returned on failure or cancellation.
		/// </remarks>
		public static async Task<byte[]> CompressAsync(Stream source, CompressionFormat format,
			int maxInputBytes, int maxOutputBytes, CompressionOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			Validate(format, maxInputBytes, maxOutputBytes, options);
			var input = await BoundedRead.ReadAllBytesAsync(source, maxInputBytes, cancellationToken).ConfigureAwait(false);
			using var output = new LimitedBufferStream(maxOutputBytes);
			if (input.Length == 0 && format == CompressionFormat.GZip)
			{
				output.Write(EmptyGZip);
			}
			else
			{
				await using var encoder = CreateEncoder(output, format, options);
				for (var offset = 0; offset < input.Length;)
				{
					cancellationToken.ThrowIfCancellationRequested();
					var count = Math.Min(BufferSize, input.Length - offset);
					await encoder.WriteAsync(input.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
					offset += count;
				}
			}
			cancellationToken.ThrowIfCancellationRequested();
			return output.ToArray();
		}

		/// <summary>Returns decoded bytes, enforcing independent compressed-input and decoded-output limits.</summary>
		/// <param name="source">Readable caller-owned stream containing the entire encoded payload.</param>
		/// <param name="format">Input format supplied by the caller. No detection occurs.</param>
		/// <param name="maxInputBytes">Inclusive encoded-input limit, from zero through Array.MaxLength.</param>
		/// <param name="maxOutputBytes">Inclusive decoded-output limit, from zero through Array.MaxLength.</param>
		/// <remarks>
		/// Consumes the entire bounded source before decoding. Empty encoded input is rejected; a valid
		/// encoding of empty content is accepted. Full format validity/integrity is not guaranteed by EOF.
		/// </remarks>
		/// <exception cref="InvalidDataException">A limit is exceeded, encoded input is empty, or the codec reports invalid data.</exception>
		public static byte[] Decompress(Stream source, CompressionFormat format, int maxInputBytes, int maxOutputBytes)
		{
			Validate(format, maxInputBytes, maxOutputBytes, options: null);
			var input = BoundedRead.ReadAllBytes(source, maxInputBytes);
			EnsureEncodedInput(input);
			using var encoded = new MemoryStream(input, writable: false);
			using var decoder = CreateDecoder(encoded, format);
			try
			{
				return BoundedRead.ReadAllBytes(decoder, maxOutputBytes);
			}
			catch (InvalidOperationException error) when (format == CompressionFormat.Brotli)
			{
				throw new InvalidDataException("The Brotli payload is invalid.", error);
			}
		}

		/// <summary>Asynchronously returns decoded bytes under the same bounds and runtime-validation contract as Decompress.</summary>
		/// <param name="source">Readable caller-owned stream containing the entire encoded payload.</param>
		/// <param name="format">Input format supplied by the caller. No detection occurs.</param>
		/// <param name="maxInputBytes">Inclusive encoded-input limit, from zero through Array.MaxLength.</param>
		/// <param name="maxOutputBytes">Inclusive decoded-output limit, from zero through Array.MaxLength.</param>
		/// <param name="cancellationToken">Cancellation during reads and before returning the result.</param>
		/// <exception cref="InvalidDataException">A limit is exceeded, encoded input is empty, or the codec reports invalid data.</exception>
		/// <exception cref="OperationCanceledException">Cancellation is observed before returning a result.</exception>
		/// <remarks>Leaves the caller source open and observes cancellation between reads. No partial payload is returned.</remarks>
		public static async Task<byte[]> DecompressAsync(Stream source, CompressionFormat format,
			int maxInputBytes, int maxOutputBytes, CancellationToken cancellationToken = default)
		{
			Validate(format, maxInputBytes, maxOutputBytes, options: null);
			var input = await BoundedRead.ReadAllBytesAsync(source, maxInputBytes, cancellationToken).ConfigureAwait(false);
			EnsureEncodedInput(input);
			using var encoded = new MemoryStream(input, writable: false);
			await using var decoder = CreateDecoder(encoded, format);
			try
			{
				return await BoundedRead.ReadAllBytesAsync(decoder, maxOutputBytes, cancellationToken).ConfigureAwait(false);
			}
			catch (InvalidOperationException error) when (format == CompressionFormat.Brotli)
			{
				throw new InvalidDataException("The Brotli payload is invalid.", error);
			}
		}

		private static void Validate(CompressionFormat format, int maxInputBytes, int maxOutputBytes, CompressionOptions? options)
		{
			if (!Enum.IsDefined(format))
				throw new ArgumentOutOfRangeException(nameof(format));
			ArgumentOutOfRangeException.ThrowIfNegative(maxInputBytes);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(maxInputBytes, Array.MaxLength);
			ArgumentOutOfRangeException.ThrowIfNegative(maxOutputBytes);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(maxOutputBytes, Array.MaxLength);
			if (options?.Level is { } level && !Enum.IsDefined(level))
				throw new ArgumentException("Level must be a defined CompressionLevel.", nameof(options));
			if (options?.BrotliQuality is { } quality)
			{
				if (format != CompressionFormat.Brotli || options.Level.HasValue)
					throw new ArgumentException("BrotliQuality requires Brotli and cannot be combined with Level.", nameof(options));
				if (quality is < 0 or > 11)
					throw new ArgumentOutOfRangeException(nameof(options), "BrotliQuality must be between 0 and 11.");
			}
		}

		private static Stream CreateEncoder(Stream output, CompressionFormat format, CompressionOptions? options)
		{
			if (format == CompressionFormat.GZip)
				return new GZipStream(output, options?.Level ?? CompressionLevel.Optimal, leaveOpen: true);
			return options?.BrotliQuality is { } quality
				? new BrotliStream(output, new BrotliCompressionOptions { Quality = quality }, leaveOpen: true)
				: new BrotliStream(output, options?.Level ?? CompressionLevel.Optimal, leaveOpen: true);
		}

		private static Stream CreateDecoder(Stream input, CompressionFormat format)
			=> format == CompressionFormat.GZip
				? new GZipStream(input, CompressionMode.Decompress, leaveOpen: true)
				: new BrotliStream(input, CompressionMode.Decompress, leaveOpen: true);

		private static void EnsureEncodedInput(byte[] input)
		{
			if (input.Length == 0)
				throw new InvalidDataException("The compressed input is empty.");
		}
	}
}
