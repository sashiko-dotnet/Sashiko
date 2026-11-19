using System.Security.Cryptography;
using Sashiko.Core.IO;

namespace Sashiko.Core.Compression
{
	public static partial class CompressedData
	{
		/// <summary>Decodes a bounded payload and verifies its exact decoded length and SHA-256 digest.</summary>
		/// <param name="source">Readable caller-owned stream, consumed from its current position and left open.</param>
		/// <param name="format">Explicit compressed input format.</param>
		/// <param name="maxInputBytes">Inclusive encoded input limit, from zero through Array.MaxLength.</param>
		/// <param name="maxOutputBytes">Inclusive decoded output limit, from zero through Array.MaxLength.</param>
		/// <param name="expectedDecodedLength">Exact decoded length, from zero through the output limit.</param>
		/// <param name="expectedDecodedSha256">Trusted 32-byte SHA-256 digest of the decoded bytes.</param>
		/// <param name="expectedEncodedSha256">Optional trusted 32-byte digest of all encoded bytes. An empty span skips this check.</param>
		/// <exception cref="InvalidDataException">A byte limit, codec, length, or digest check fails.</exception>
		/// <remarks>
		/// Copies expected digests before reading. Verifies the encoded digest before decoding when supplied
		/// and applies the expected length as the decoding limit. Hash verification compares bytes against
		/// the supplied expectations. Codec framing follows the same runtime behavior as Decompress.
		/// </remarks>
		public static byte[] DecompressVerified(Stream source, CompressionFormat format,
			int maxInputBytes, int maxOutputBytes, int expectedDecodedLength,
			ReadOnlySpan<byte> expectedDecodedSha256, ReadOnlySpan<byte> expectedEncodedSha256 = default)
		{
			ValidateVerification(format, maxInputBytes, maxOutputBytes, expectedDecodedLength,
				expectedDecodedSha256, expectedEncodedSha256);
			var decodedHash = expectedDecodedSha256.ToArray();
			var encodedHash = expectedEncodedSha256.ToArray();

			var input = BoundedRead.ReadAllBytes(source, maxInputBytes);
			byte[] decoded;
			try
			{
				VerifyEncodedHash(input, encodedHash);
				decoded = Decode(input, format, expectedDecodedLength);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(input);
			}
			return VerifyDecodedOutput(decoded, expectedDecodedLength, decodedHash);
		}

		/// <summary>Asynchronously decodes and verifies a bounded payload under the DecompressVerified contract.</summary>
		/// <param name="source">Readable caller-owned stream, consumed from its current position and left open.</param>
		/// <param name="format">Explicit compressed input format.</param>
		/// <param name="maxInputBytes">Inclusive encoded input limit, from zero through Array.MaxLength.</param>
		/// <param name="maxOutputBytes">Inclusive decoded output limit, from zero through Array.MaxLength.</param>
		/// <param name="expectedDecodedLength">Exact decoded length, from zero through the output limit.</param>
		/// <param name="expectedDecodedSha256">Trusted 32-byte SHA-256 digest of the decoded bytes.</param>
		/// <param name="expectedEncodedSha256">Optional trusted 32-byte digest of all encoded bytes. Empty memory skips this check.</param>
		/// <param name="cancellationToken">Cancellation during reading, between stages, and before returning.</param>
		/// <exception cref="InvalidDataException">A byte limit, codec, length, or digest check fails.</exception>
		/// <exception cref="OperationCanceledException">Cancellation is observed before returning a result.</exception>
		/// <remarks>Hashing is synchronous. Cancellation is observed between work stages, including after successful verification.</remarks>
		public static async Task<byte[]> DecompressVerifiedAsync(Stream source, CompressionFormat format,
			int maxInputBytes, int maxOutputBytes, int expectedDecodedLength,
			ReadOnlyMemory<byte> expectedDecodedSha256, ReadOnlyMemory<byte> expectedEncodedSha256 = default,
			CancellationToken cancellationToken = default)
		{
			ValidateVerification(format, maxInputBytes, maxOutputBytes, expectedDecodedLength,
				expectedDecodedSha256.Span, expectedEncodedSha256.Span);
			cancellationToken.ThrowIfCancellationRequested();
			var decodedHash = expectedDecodedSha256.ToArray();
			var encodedHash = expectedEncodedSha256.ToArray();

			var input = await BoundedRead.ReadAllBytesAsync(source, maxInputBytes, cancellationToken).ConfigureAwait(false);
			byte[] decoded;
			try
			{
				VerifyEncodedHash(input, encodedHash);
				cancellationToken.ThrowIfCancellationRequested();
				decoded = await DecodeAsync(input, format, expectedDecodedLength, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(input);
			}
			return VerifyDecodedOutput(decoded, expectedDecodedLength, decodedHash, cancellationToken);
		}

		private static void ValidateVerification(CompressionFormat format, int maxInputBytes, int maxOutputBytes,
			int expectedDecodedLength, ReadOnlySpan<byte> expectedDecodedSha256, ReadOnlySpan<byte> expectedEncodedSha256)
		{
			Validate(format, maxInputBytes, maxOutputBytes, options: null);
			ArgumentOutOfRangeException.ThrowIfNegative(expectedDecodedLength);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(expectedDecodedLength, maxOutputBytes);
			if (expectedDecodedSha256.Length != SHA256.HashSizeInBytes)
				throw new ArgumentException("The expected decoded SHA-256 digest must contain 32 bytes.", nameof(expectedDecodedSha256));
			if (!expectedEncodedSha256.IsEmpty && expectedEncodedSha256.Length != SHA256.HashSizeInBytes)
				throw new ArgumentException("The expected encoded SHA-256 digest must contain 32 bytes.", nameof(expectedEncodedSha256));
		}

		private static void VerifyEncodedHash(ReadOnlySpan<byte> input, ReadOnlySpan<byte> expectedHash)
		{
			if (!expectedHash.IsEmpty && !MatchesSha256(input, expectedHash))
				throw new InvalidDataException("The encoded payload does not match the expected SHA-256 digest.");
		}

		private static byte[] VerifyDecodedOutput(byte[] decoded, int expectedLength, ReadOnlySpan<byte> expectedHash,
			CancellationToken cancellationToken = default)
		{
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (decoded.Length != expectedLength)
					throw new InvalidDataException("The decoded payload does not match the expected length.");
				if (!MatchesSha256(decoded, expectedHash))
					throw new InvalidDataException("The decoded payload does not match the expected SHA-256 digest.");

				cancellationToken.ThrowIfCancellationRequested();
				return decoded;
			}
			catch
			{
				CryptographicOperations.ZeroMemory(decoded);
				throw;
			}
		}

		private static bool MatchesSha256(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> expectedHash)
		{
			Span<byte> actualHash = stackalloc byte[SHA256.HashSizeInBytes];
			SHA256.HashData(bytes, actualHash);
			return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
		}
	}
}
