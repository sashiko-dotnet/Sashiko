using System.Security.Cryptography;
using Sashiko.Core.Compression;
using Sashiko.Core.Tests.IO;

namespace Sashiko.Core.Tests.Compression
{
	public sealed class VerifiedDecompressionTests
	{
		private static readonly byte[] Payload = "Fictional saved document"u8.ToArray();

		[Theory]
		[InlineData(CompressionFormat.GZip, false)]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		[InlineData(CompressionFormat.Brotli, true)]
		public async Task ValidDigests_AreSnapshottedBeforeReading(CompressionFormat format, bool asynchronous)
		{
			var encoded = Encode(Payload, format);
			var decodedHash = SHA256.HashData(Payload);
			var encodedHash = SHA256.HashData(encoded);
			using var source = new ProbeReadStream(encoded, _ =>
			{
				Array.Clear(decodedHash);
				Array.Clear(encodedHash);
			});

			var restored = await Decode(source, format, asynchronous, encoded.Length, Payload.Length,
				Payload.Length, decodedHash, encodedHash);

			Assert.Equal(Payload, restored);
			Assert.Equal(encoded.Length, source.BytesRead);
			Assert.False(source.IsDisposed);
		}

		[Theory]
		[InlineData(CompressionFormat.GZip)]
		[InlineData(CompressionFormat.Brotli)]
		public async Task EmptyContent_WithAValidEncodingAndDigest_IsAccepted(CompressionFormat format)
		{
			var encoded = Encode([], format);
			using var source = new MemoryStream(encoded);

			var restored = await CompressedData.DecompressVerifiedAsync(source, format, encoded.Length,
				maxOutputBytes: 0, expectedDecodedLength: 0, expectedDecodedSha256: SHA256.HashData([]));

			Assert.Empty(restored);
		}

		[Theory]
		[InlineData(CompressionFormat.GZip, true)]
		[InlineData(CompressionFormat.Brotli, false)]
		public async Task DecodedDigestMismatch_IsRejectedEvenWhenLengthAndEncodedDigestMatch(
			CompressionFormat format, bool asynchronous)
		{
			var encoded = Encode(Payload, format);
			using var source = new ProbeReadStream(encoded);

			var error = await Assert.ThrowsAsync<InvalidDataException>(() => Decode(source, format, asynchronous,
				encoded.Length, Payload.Length, Payload.Length, new byte[SHA256.HashSizeInBytes], SHA256.HashData(encoded)));

			Assert.Contains("decoded payload", error.Message);
			Assert.Contains("SHA-256", error.Message);
			Assert.False(source.IsDisposed);
		}

		[Theory]
		[InlineData(-1, false)]
		[InlineData(-1, true)]
		[InlineData(1, false)]
		[InlineData(1, true)]
		public async Task DeclaredLength_MustMatchActualBytesEvenWithTheCorrectHash(int lengthDifference, bool asynchronous)
		{
			var encoded = Encode(Payload, CompressionFormat.Brotli);
			using var source = new ProbeReadStream(encoded);

			await Assert.ThrowsAsync<InvalidDataException>(() => Decode(source, CompressionFormat.Brotli, asynchronous,
				encoded.Length, Payload.Length + 8, Payload.Length + lengthDifference, SHA256.HashData(Payload)));

			Assert.False(source.IsDisposed);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task WrongEncodedDigest_IsReportedBeforeAttemptingToDecode(bool asynchronous)
		{
			using var source = new ProbeReadStream("invalid gzip"u8.ToArray());

			var error = await Assert.ThrowsAsync<InvalidDataException>(() => Decode(source, CompressionFormat.GZip, asynchronous,
				64, Payload.Length, Payload.Length, SHA256.HashData(Payload), new byte[SHA256.HashSizeInBytes]));

			Assert.Contains("encoded payload", error.Message);
			Assert.Contains("SHA-256", error.Message);
			Assert.False(source.IsDisposed);
		}

		[Fact]
		public void EncodedDigest_CoversRepresentationChangesThatPreserveDecodedBytes()
		{
			var encoded = Encode(Payload, CompressionFormat.GZip);
			var originalHash = SHA256.HashData(encoded);
			// Change the GZip header's timestamp field without changing the encoded content.
			encoded[4] ^= 1;
			var decodedHash = SHA256.HashData(Payload);
			using var source = new MemoryStream(encoded);

			Assert.Equal(Payload, CompressedData.DecompressVerified(source, CompressionFormat.GZip, encoded.Length,
				Payload.Length, Payload.Length, decodedHash));
			source.Position = 0;
			Assert.Throws<InvalidDataException>(() => CompressedData.DecompressVerified(source, CompressionFormat.GZip,
				encoded.Length, Payload.Length, Payload.Length, decodedHash, originalHash));
		}

		[Fact]
		public void EncodedLimit_IsEnforcedEvenWithMatchingDigests()
		{
			var encoded = Encode(Payload, CompressionFormat.GZip);
			using var source = new ProbeReadStream(encoded);

			Assert.Throws<InvalidDataException>(() => CompressedData.DecompressVerified(source, CompressionFormat.GZip,
				encoded.Length - 2, Payload.Length, Payload.Length, SHA256.HashData(Payload), SHA256.HashData(encoded)));

			Assert.Equal(encoded.Length - 1, source.BytesRead);
			Assert.False(source.IsDisposed);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task InvalidExpectations_AreRejectedBeforeReading(bool asynchronous)
		{
			using var source = new ProbeReadStream(Payload);
			var hash = SHA256.HashData(Payload);
			var oversized = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Decode(source,
				CompressionFormat.Brotli, asynchronous, 64, 8, 9, hash));
			Assert.Equal("expectedDecodedLength", oversized.ParamName);
			var negative = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Decode(source,
				CompressionFormat.Brotli, asynchronous, 64, 8, -1, hash));
			Assert.Equal("expectedDecodedLength", negative.ParamName);
			var invalidDecodedHash = await Assert.ThrowsAsync<ArgumentException>(() => Decode(source,
				CompressionFormat.Brotli, asynchronous, 64, 8, 8, []));
			Assert.Equal("expectedDecodedSha256", invalidDecodedHash.ParamName);
			var invalidEncodedHash = await Assert.ThrowsAsync<ArgumentException>(() => Decode(source,
				CompressionFormat.Brotli, asynchronous, 64, 8, 8, hash, new byte[31]));
			Assert.Equal("expectedEncodedSha256", invalidEncodedHash.ParamName);
			Assert.Equal(0, source.BytesRead);
			Assert.False(source.IsDisposed);
		}

		[Fact]
		public async Task AsyncCancellation_LeavesTheSourceOpen()
		{
			var encoded = Encode(Payload, CompressionFormat.GZip);
			using var cancellation = new CancellationTokenSource();
			using var source = new ProbeReadStream(encoded, count =>
			{
				if (count > 0)
					cancellation.Cancel();
			});

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CompressedData.DecompressVerifiedAsync(
				source, CompressionFormat.GZip, encoded.Length, Payload.Length, Payload.Length,
				SHA256.HashData(Payload), cancellationToken: cancellation.Token));

			Assert.InRange(source.BytesRead, 1, encoded.Length - 1);
			Assert.False(source.IsDisposed);
		}

		private static byte[] Encode(byte[] bytes, CompressionFormat format)
		{
			using var source = new MemoryStream(bytes);
			return CompressedData.Compress(source, format, bytes.Length, 256);
		}

		private static Task<byte[]> Decode(Stream source, CompressionFormat format, bool asynchronous,
			int maxInputBytes, int maxOutputBytes, int expectedLength, byte[] decodedHash, byte[]? encodedHash = null)
			=> asynchronous
				? CompressedData.DecompressVerifiedAsync(source, format, maxInputBytes, maxOutputBytes, expectedLength, decodedHash, encodedHash)
				: Task.FromResult(CompressedData.DecompressVerified(source, format, maxInputBytes, maxOutputBytes,
					expectedLength, decodedHash, encodedHash));
	}
}
