using System.IO.Compression;
using System.Text;
using Sashiko.Core.Compression;
using Sashiko.Core.Tests.IO;

namespace Sashiko.Core.Tests.Compression
{
	public sealed class CompressedDataTests
	{
		private static readonly byte[] Payload = Encoding.UTF8.GetBytes("{\"label\":\"é日本語\",\"value\":123}\n");

		[Theory]
		[InlineData(CompressionFormat.GZip, false)]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		[InlineData(CompressionFormat.Brotli, true)]
		public async Task RoundTrip_PreservesExactBytes_AndLeavesNonSeekableSourcesOpen(CompressionFormat format, bool asynchronous)
		{
			var bytes = Enumerable.Range(0, 220_000).Select(i => (byte)i).ToArray();
			using var source = new ProbeReadStream(bytes, chunkSize: 4093);

			var compressed = await Compress(source, format, asynchronous, bytes.Length, bytes.Length + 1024);
			using var encoded = new ProbeReadStream(compressed);
			var restored = await Decompress(encoded, format, asynchronous, compressed.Length, bytes.Length);

			Assert.Equal(bytes, restored);
			Assert.Equal(bytes, DecodeDirect(compressed, format));
			Assert.Equal(bytes.Length, source.BytesRead);
			Assert.Equal(compressed.Length, encoded.BytesRead);
			Assert.False(source.IsDisposed);
			Assert.False(encoded.IsDisposed);
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, false)]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		[InlineData(CompressionFormat.Brotli, true)]
		public async Task EmptyContent_HasAValidEncoding_AndDecodesUnderZeroOutputLimit(CompressionFormat format, bool asynchronous)
		{
			using var source = new ProbeReadStream([]);
			var compressed = await Compress(source, format, asynchronous, 0, 128);
			Assert.NotEmpty(compressed);
			using var encoded = new ProbeReadStream(compressed);

			Assert.Empty(await Decompress(encoded, format, asynchronous, compressed.Length, 0));
			Assert.Empty(DecodeDirect(compressed, format));
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, false)]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		[InlineData(CompressionFormat.Brotli, true)]
		public async Task Operations_UseCurrentSourcePosition_AndDoNotRewind(CompressionFormat format, bool asynchronous)
		{
			using var source = new MemoryStream([0, 0, .. Payload]);
			source.Position = 2;
			var compressed = await Compress(source, format, asynchronous, Payload.Length, 256);
			using var encoded = new MemoryStream([0, 0, .. compressed]);
			encoded.Position = 2;

			Assert.Equal(Payload, await Decompress(encoded, format, asynchronous, compressed.Length, Payload.Length));
			Assert.Equal(source.Length, source.Position);
			Assert.Equal(encoded.Length, encoded.Position);
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, CompressionLevel.Fastest)]
		[InlineData(CompressionFormat.GZip, CompressionLevel.Optimal)]
		[InlineData(CompressionFormat.GZip, CompressionLevel.SmallestSize)]
		[InlineData(CompressionFormat.GZip, CompressionLevel.NoCompression)]
		[InlineData(CompressionFormat.Brotli, CompressionLevel.Fastest)]
		[InlineData(CompressionFormat.Brotli, CompressionLevel.Optimal)]
		[InlineData(CompressionFormat.Brotli, CompressionLevel.SmallestSize)]
		[InlineData(CompressionFormat.Brotli, CompressionLevel.NoCompression)]
		public async Task CompressionLevel_UsesTheSelectedRuntimePreference(CompressionFormat format, CompressionLevel level)
		{
			var options = new CompressionOptions { Level = level };
			using var input = new MemoryStream(Payload);
			var compressed = CompressedData.Compress(input, format, Payload.Length, 256, options);
			input.Position = 0;

			Assert.Equal(EncodeDirect(Payload, format, options), compressed);
			Assert.Equal(compressed, await CompressedData.CompressAsync(input, format, Payload.Length, 256, options));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(4)]
		[InlineData(11)]
		public async Task ExplicitBrotliQuality_UsesTheSelectedRuntimeSetting(int quality)
		{
			var options = new CompressionOptions { BrotliQuality = quality };
			using var input = new MemoryStream(Payload);
			var compressed = CompressedData.Compress(input, CompressionFormat.Brotli, Payload.Length, 256, options);
			input.Position = 0;

			Assert.Equal(EncodeDirect(Payload, CompressionFormat.Brotli, options), compressed);
			Assert.Equal(compressed, await CompressedData.CompressAsync(input, CompressionFormat.Brotli, Payload.Length, 256, options));
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, false)]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		[InlineData(CompressionFormat.Brotli, true)]
		public async Task CompressionOutputLimit_IncludesFinalizationBytes(CompressionFormat format, bool asynchronous)
		{
			var exact = EncodeDirect(Payload, format).Length;
			using var input = new ProbeReadStream(Payload);
			Assert.Equal(exact, (await Compress(input, format, asynchronous, Payload.Length, exact)).Length);
			using var tooLarge = new ProbeReadStream(Payload);

			await Assert.ThrowsAsync<InvalidDataException>(() => Compress(tooLarge, format, asynchronous, Payload.Length, exact - 1));
			Assert.False(tooLarge.IsDisposed);
			using var empty = new ProbeReadStream([]);
			await Assert.ThrowsAsync<InvalidDataException>(() => Compress(empty, format, asynchronous, 0, 0));
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, false)]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		[InlineData(CompressionFormat.Brotli, true)]
		public async Task CompressionOutputLimits_RejectHeaderMidstreamAndTrailerCuts(CompressionFormat format, bool asynchronous)
		{
			var bytes = new byte[128_000];
			new Random(42).NextBytes(bytes);
			var options = new CompressionOptions { Level = CompressionLevel.Fastest };
			using var initial = new MemoryStream(bytes);
			var encoded = await Compress(initial, format, asynchronous, bytes.Length, bytes.Length * 2, options);

			foreach (var limit in new[] { 0, 1, encoded.Length / 2, encoded.Length - 1 })
			{
				using var source = new ProbeReadStream(bytes, chunkSize: 4093);
				await Assert.ThrowsAsync<InvalidDataException>(() =>
					Compress(source, format, asynchronous, bytes.Length, limit, options));
				Assert.False(source.IsDisposed);
				Assert.Equal(bytes.Length, source.BytesRead);
			}
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, false)]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		[InlineData(CompressionFormat.Brotli, true)]
		public async Task InputLimits_CountActualBytes_ForBothOperations(CompressionFormat format, bool asynchronous)
		{
			using var raw = new ProbeReadStream(Payload, chunkSize: Payload.Length);
			await Assert.ThrowsAsync<InvalidDataException>(() => Compress(raw, format, asynchronous, 2, 256));
			Assert.Equal(3, raw.BytesRead);
			Assert.False(raw.IsDisposed);

			var compressed = EncodeDirect(Payload, format);
			using var encoded = new ProbeReadStream(compressed, chunkSize: compressed.Length);
			await Assert.ThrowsAsync<InvalidDataException>(() => Decompress(encoded, format, asynchronous, 2, 256));
			Assert.Equal(3, encoded.BytesRead);
			Assert.False(encoded.IsDisposed);
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, false)]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		[InlineData(CompressionFormat.Brotli, true)]
		public async Task DecodedOutputLimit_RejectsExpansionBeyondTheLimit(CompressionFormat format, bool asynchronous)
		{
			var compressed = EncodeDirect(new byte[100_000], format);
			using var input = new ProbeReadStream(compressed);

			await Assert.ThrowsAsync<InvalidDataException>(() => Decompress(input, format, asynchronous, compressed.Length, 1000));
			Assert.False(input.IsDisposed);
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, false)]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		[InlineData(CompressionFormat.Brotli, true)]
		public async Task Decoding_RejectsEmptyInputAndCodecReportedInvalidData(CompressionFormat format, bool asynchronous)
		{
			using var empty = new ProbeReadStream([]);
			await Assert.ThrowsAsync<InvalidDataException>(() => Decompress(empty, format, asynchronous, 0, 128));
			using var invalid = new ProbeReadStream([255, 255, 255, 255, 255]);
			await Assert.ThrowsAsync<InvalidDataException>(() => Decompress(invalid, format, asynchronous, 5, 128));
			Assert.False(empty.IsDisposed);
			Assert.False(invalid.IsDisposed);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task GZipCorruption_IsReportedByTheRuntimeChecksum(bool asynchronous)
		{
			var compressed = EncodeDirect(Payload, CompressionFormat.GZip);
			compressed[^8] ^= 0xff;
			using var input = new ProbeReadStream(compressed);

			await Assert.ThrowsAsync<InvalidDataException>(() =>
				Decompress(input, CompressionFormat.GZip, asynchronous, compressed.Length, 256));
			Assert.False(input.IsDisposed);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task SourceInvalidOperationException_IsNotReclassifiedAsBrotliInvalidData(bool asynchronous)
		{
			var failure = new InvalidOperationException("Synthetic source failure.");
			using var input = new ProbeReadStream(Payload, _ => throw failure);

			var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				Decompress(input, CompressionFormat.Brotli, asynchronous, 256, 256));

			Assert.Same(failure, error);
			Assert.False(input.IsDisposed);
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, false)]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		[InlineData(CompressionFormat.Brotli, true)]
		public async Task TruncationTrailingDataAndConcatenation_FollowRuntimeReadSemantics(CompressionFormat format, bool asynchronous)
		{
			var valid = EncodeDirect(Payload, format);
			foreach (var data in new byte[][] { valid[..^1], [.. valid, 1, 2, 3], [.. valid, .. valid] })
			{
				byte[] expected;
				try { expected = DecodeDirect(data, format); }
				catch (InvalidDataException)
				{
					using var invalid = new ProbeReadStream(data);
					await Assert.ThrowsAsync<InvalidDataException>(() => Decompress(invalid, format, asynchronous, data.Length, 256));
					continue;
				}
				using var input = new ProbeReadStream(data);
				Assert.Equal(expected, await Decompress(input, format, asynchronous, data.Length, 256));
				Assert.Equal(data.Length, input.BytesRead);
			}
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, false)]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		[InlineData(CompressionFormat.Brotli, true)]
		public async Task InvalidArguments_DoNotReadTheSource(CompressionFormat format, bool asynchronous)
		{
			var reads = 0;
			using var source = new ProbeReadStream(Payload, _ => reads++);
			foreach (var (inputLimit, outputLimit) in new[] { (-1, 10), (10, -1), (Array.MaxLength + 1, 10), (10, Array.MaxLength + 1) })
			{
				await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Compress(source, format, asynchronous, inputLimit, outputLimit));
				await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Decompress(source, format, asynchronous, inputLimit, outputLimit));
			}
			await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Compress(source, (CompressionFormat)123, asynchronous, 256, 256));
			await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Decompress(source, (CompressionFormat)123, asynchronous, 256, 256));
			Assert.Equal(0, reads);
			Assert.False(source.IsDisposed);
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, false)]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		[InlineData(CompressionFormat.Brotli, true)]
		public async Task InvalidSources_AreRejected(CompressionFormat format, bool asynchronous)
		{
			await Assert.ThrowsAsync<ArgumentNullException>(() => Compress(null!, format, asynchronous, 256, 256));
			await Assert.ThrowsAsync<ArgumentNullException>(() => Decompress(null!, format, asynchronous, 256, 256));
			using var disposed = new MemoryStream(Payload);
			disposed.Dispose();
			await Assert.ThrowsAsync<ArgumentException>(() => Compress(disposed, format, asynchronous, 256, 256));
			await Assert.ThrowsAsync<ArgumentException>(() => Decompress(disposed, format, asynchronous, 256, 256));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task InvalidOrConflictingEffortOptions_AreRejectedBeforeReading(bool asynchronous)
		{
			using var source = new ProbeReadStream(Payload);
			foreach (var options in new[]
			{
				new CompressionOptions { Level = (CompressionLevel)123 },
				new CompressionOptions { BrotliQuality = -1 },
				new CompressionOptions { BrotliQuality = 12 },
				new CompressionOptions { BrotliQuality = 4, Level = CompressionLevel.Optimal }
			})
			{
				await Assert.ThrowsAnyAsync<ArgumentException>(() => Compress(source, CompressionFormat.Brotli, asynchronous, 256, 256, options));
			}
			await Assert.ThrowsAsync<ArgumentException>(() =>
				Compress(source, CompressionFormat.GZip, asynchronous, 256, 256, new CompressionOptions { BrotliQuality = 4 }));
			Assert.Equal(0, source.BytesRead);
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, false)]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		[InlineData(CompressionFormat.Brotli, true)]
		public async Task SourceFailure_PreservesTheOriginalExceptionAndOwnership(CompressionFormat format, bool asynchronous)
		{
			var failure = new IOException("Synthetic source failure.");
			foreach (var decompress in new[] { false, true })
			{
				using var source = new ProbeReadStream(Payload, count =>
				{
					if (count > 0)
						throw failure;
				});
				var error = await Assert.ThrowsAsync<IOException>(() => decompress
					? Decompress(source, format, asynchronous, 256, 256)
					: Compress(source, format, asynchronous, 256, 256));
				Assert.Same(failure, error);
				Assert.False(source.IsDisposed);
			}
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, -1)]
		[InlineData(CompressionFormat.GZip, 2)]
		[InlineData(CompressionFormat.GZip, int.MaxValue)]
		[InlineData(CompressionFormat.Brotli, -1)]
		[InlineData(CompressionFormat.Brotli, 2)]
		[InlineData(CompressionFormat.Brotli, int.MaxValue)]
		public async Task AsyncCancellation_LeavesTheSourceOpen(CompressionFormat format, int cancelAfter)
		{
			foreach (var decompress in new[] { false, true })
			{
				using var cancellation = new CancellationTokenSource();
				if (cancelAfter < 0)
					cancellation.Cancel();
				using var source = new ProbeReadStream(Payload, count =>
				{
					if (count == (cancelAfter == int.MaxValue ? Payload.Length : cancelAfter))
						cancellation.Cancel();
				});
				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => decompress
					? CompressedData.DecompressAsync(source, format, 256, 256, cancellation.Token)
					: CompressedData.CompressAsync(source, format, 256, 256, cancellationToken: cancellation.Token));
				Assert.False(source.IsDisposed);
				if (cancelAfter < 0)
					Assert.Equal(0, source.BytesRead);
			}
		}

		private static Task<byte[]> Compress(Stream source, CompressionFormat format, bool asynchronous,
			int maxInput, int maxOutput, CompressionOptions? options = null)
			=> asynchronous ? CompressedData.CompressAsync(source, format, maxInput, maxOutput, options)
				: Task.FromResult(CompressedData.Compress(source, format, maxInput, maxOutput, options));

		private static Task<byte[]> Decompress(Stream source, CompressionFormat format, bool asynchronous, int maxInput, int maxOutput)
			=> asynchronous ? CompressedData.DecompressAsync(source, format, maxInput, maxOutput)
				: Task.FromResult(CompressedData.Decompress(source, format, maxInput, maxOutput));

		private static byte[] EncodeDirect(byte[] input, CompressionFormat format, CompressionOptions? options = null)
		{
			using var output = new MemoryStream();
			using (Stream encoder = format == CompressionFormat.GZip
				? new GZipStream(output, options?.Level ?? CompressionLevel.Optimal, leaveOpen: true)
				: options?.BrotliQuality is { } quality
					? new BrotliStream(output, new BrotliCompressionOptions { Quality = quality }, leaveOpen: true)
					: new BrotliStream(output, options?.Level ?? CompressionLevel.Optimal, leaveOpen: true))
				encoder.Write(input);
			return output.ToArray();
		}

		private static byte[] DecodeDirect(byte[] input, CompressionFormat format)
		{
			using var encoded = new MemoryStream(input);
			using Stream decoder = format == CompressionFormat.GZip
				? new GZipStream(encoded, CompressionMode.Decompress)
				: new BrotliStream(encoded, CompressionMode.Decompress);
			using var output = new MemoryStream();
			var buffer = new byte[80 * 1024];
			int read;
			while ((read = decoder.Read(buffer)) != 0)
				output.Write(buffer, 0, read);
			return output.ToArray();
		}
	}
}
