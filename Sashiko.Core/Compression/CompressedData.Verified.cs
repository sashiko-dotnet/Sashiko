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
		/// <param name="verification">Expected length and digests. The length must fit within the output limit.</param>
		/// <exception cref="InvalidDataException">A byte limit, codec, length, or digest check fails.</exception>
		/// <remarks>
		/// Verifies the encoded digest before decoding when supplied
		/// and applies the expected length as the decoding limit. Hash verification compares bytes against
		/// the supplied expectations. Codec framing follows the same runtime behavior as Decompress.
		/// </remarks>
		public static byte[] DecompressVerified(Stream source, CompressionFormat format,
			int maxInputBytes, int maxOutputBytes, DecompressionVerification verification)
		{
			ValidateVerification(format, maxInputBytes, maxOutputBytes, verification);

			var input = BoundedRead.ReadAllBytes(source, maxInputBytes);
			byte[] decoded;
			try
			{
				VerifyEncodedHash(input, verification.EncodedSha256);
				decoded = Decode(input, format, verification.ExpectedDecodedLength);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(input);
			}
			return VerifyDecodedOutput(decoded, verification);
		}

		/// <summary>Asynchronously decodes and verifies a bounded payload under the DecompressVerified contract.</summary>
		/// <param name="source">Readable caller-owned stream, consumed from its current position and left open.</param>
		/// <param name="format">Explicit compressed input format.</param>
		/// <param name="maxInputBytes">Inclusive encoded input limit, from zero through Array.MaxLength.</param>
		/// <param name="maxOutputBytes">Inclusive decoded output limit, from zero through Array.MaxLength.</param>
		/// <param name="verification">Expected length and digests. The length must fit within the output limit.</param>
		/// <param name="cancellationToken">Cancellation during reading, between stages, and before returning.</param>
		/// <exception cref="InvalidDataException">A byte limit, codec, length, or digest check fails.</exception>
		/// <exception cref="OperationCanceledException">Cancellation is observed before returning a result.</exception>
		/// <remarks>Hashing is synchronous. Cancellation is observed between work stages, including after successful verification.</remarks>
		public static async Task<byte[]> DecompressVerifiedAsync(Stream source, CompressionFormat format,
			int maxInputBytes, int maxOutputBytes, DecompressionVerification verification, CancellationToken cancellationToken = default)
		{
			ValidateVerification(format, maxInputBytes, maxOutputBytes, verification);
			cancellationToken.ThrowIfCancellationRequested();

			var input = await BoundedRead.ReadAllBytesAsync(source, maxInputBytes, cancellationToken).ConfigureAwait(false);
			byte[] decoded;
			try
			{
				VerifyEncodedHash(input, verification.EncodedSha256);
				cancellationToken.ThrowIfCancellationRequested();
				decoded = await DecodeAsync(input, format, verification.ExpectedDecodedLength, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(input);
			}
			return VerifyDecodedOutput(decoded, verification, cancellationToken);
		}

		private static void ValidateVerification(CompressionFormat format, int maxInputBytes, int maxOutputBytes,
			DecompressionVerification verification)
		{
			Validate(format, maxInputBytes, maxOutputBytes, options: null);
			ArgumentNullException.ThrowIfNull(verification);
			if (verification.ExpectedDecodedLength > maxOutputBytes)
				throw new ArgumentOutOfRangeException(nameof(verification), "The expected decoded length exceeds the output limit.");
		}

		private static void VerifyEncodedHash(ReadOnlySpan<byte> input, ReadOnlySpan<byte> expectedHash)
		{
			if (!expectedHash.IsEmpty && !MatchesSha256(input, expectedHash))
				throw new InvalidDataException("The encoded payload does not match the expected SHA-256 digest.");
		}

		private static byte[] VerifyDecodedOutput(byte[] decoded, DecompressionVerification verification,
			CancellationToken cancellationToken = default)
		{
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (decoded.Length != verification.ExpectedDecodedLength)
					throw new InvalidDataException("The decoded payload does not match the expected length.");
				if (!MatchesSha256(decoded, verification.DecodedSha256))
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
