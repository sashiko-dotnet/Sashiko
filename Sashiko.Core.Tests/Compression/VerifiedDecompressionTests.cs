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
			var verification = new DecompressionVerification(Payload.Length, decodedHash, encodedHash);
			using var source = new ProbeReadStream(encoded, _ =>
			{
				Array.Clear(decodedHash);
				Array.Clear(encodedHash);
			});

			var restored = await Decode(source, format, asynchronous, encoded.Length, Payload.Length, verification);

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
				maxOutputBytes: 0, new DecompressionVerification(0, SHA256.HashData([])));

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
			var verification = new DecompressionVerification(Payload.Length, new byte[SHA256.HashSizeInBytes], SHA256.HashData(encoded));

			var error = await Assert.ThrowsAsync<InvalidDataException>(() => Decode(source, format, asynchronous,
				encoded.Length, Payload.Length, verification));

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
			var verification = new DecompressionVerification(Payload.Length + lengthDifference, SHA256.HashData(Payload));

			await Assert.ThrowsAsync<InvalidDataException>(() => Decode(source, CompressionFormat.Brotli, asynchronous,
				encoded.Length, Payload.Length + 8, verification));

			Assert.False(source.IsDisposed);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task WrongEncodedDigest_IsReportedBeforeAttemptingToDecode(bool asynchronous)
		{
			using var source = new ProbeReadStream("invalid gzip"u8.ToArray());
			var verification = new DecompressionVerification(Payload.Length, SHA256.HashData(Payload), new byte[SHA256.HashSizeInBytes]);

			var error = await Assert.ThrowsAsync<InvalidDataException>(() => Decode(source, CompressionFormat.GZip, asynchronous,
				64, Payload.Length, verification));

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
				Payload.Length, new DecompressionVerification(Payload.Length, decodedHash)));
			source.Position = 0;
			Assert.Throws<InvalidDataException>(() => CompressedData.DecompressVerified(source, CompressionFormat.GZip,
				encoded.Length, Payload.Length, new DecompressionVerification(Payload.Length, decodedHash, originalHash)));
		}

		[Fact]
		public void EncodedLimit_IsEnforcedEvenWithMatchingDigests()
		{
			var encoded = Encode(Payload, CompressionFormat.GZip);
			using var source = new ProbeReadStream(encoded);
			var verification = new DecompressionVerification(Payload.Length, SHA256.HashData(Payload), SHA256.HashData(encoded));

			Assert.Throws<InvalidDataException>(() => CompressedData.DecompressVerified(source, CompressionFormat.GZip,
				encoded.Length - 2, Payload.Length, verification));

			Assert.Equal(encoded.Length - 1, source.BytesRead);
			Assert.False(source.IsDisposed);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task MissingOrOversizedExpectations_AreRejectedBeforeReading(bool asynchronous)
		{
			using var source = new ProbeReadStream(Payload);
			var verification = new DecompressionVerification(9, SHA256.HashData(Payload));
			var oversized = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Decode(source,
				CompressionFormat.Brotli, asynchronous, 64, 8, verification));
			Assert.Equal("verification", oversized.ParamName);
			await Assert.ThrowsAsync<ArgumentNullException>(() => Decode(source,
				CompressionFormat.Brotli, asynchronous, 64, 8, null!));
			Assert.Equal(0, source.BytesRead);
			Assert.False(source.IsDisposed);
		}

		[Fact]
		public void Verification_RejectsInvalidLengthsAndDigests()
		{
			var hash = SHA256.HashData(Payload);
			Assert.Throws<ArgumentOutOfRangeException>("expectedDecodedLength", () => new DecompressionVerification(-1, hash));
			Assert.Throws<ArgumentOutOfRangeException>("expectedDecodedLength", () => new DecompressionVerification(int.MaxValue, hash));
			Assert.Throws<ArgumentException>("expectedDecodedSha256", () => new DecompressionVerification(8, []));
			Assert.Throws<ArgumentException>("expectedEncodedSha256", () => new DecompressionVerification(8, hash, new byte[31]));
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
				source, CompressionFormat.GZip, encoded.Length, Payload.Length,
				new DecompressionVerification(Payload.Length, SHA256.HashData(Payload)), cancellation.Token));

			Assert.InRange(source.BytesRead, 1, encoded.Length - 1);
			Assert.False(source.IsDisposed);
		}

		private static byte[] Encode(byte[] bytes, CompressionFormat format)
		{
			using var source = new MemoryStream(bytes);
			return CompressedData.Compress(source, format, bytes.Length, 256);
		}

		private static Task<byte[]> Decode(Stream source, CompressionFormat format, bool asynchronous,
			int maxInputBytes, int maxOutputBytes, DecompressionVerification verification)
			=> asynchronous
				? CompressedData.DecompressVerifiedAsync(source, format, maxInputBytes, maxOutputBytes, verification)
				: Task.FromResult(CompressedData.DecompressVerified(source, format, maxInputBytes, maxOutputBytes, verification));
	}
}
