using System.Security.Cryptography;

namespace Sashiko.Core.Compression
{
	/// <summary>Holds the expected length and SHA-256 digests for verified decompression.</summary>
	public sealed class DecompressionVerification
	{
		private readonly byte[] _decodedSha256;
		private readonly byte[] _encodedSha256;

		/// <summary>Copies the expected digests so they can be reused across calls.</summary>
		/// <param name="expectedDecodedLength">Exact decoded byte count, from zero through Array.MaxLength.</param>
		/// <param name="expectedDecodedSha256">Trusted 32-byte SHA-256 digest of the decoded bytes.</param>
		/// <param name="expectedEncodedSha256">Optional trusted 32-byte digest of all encoded bytes. An empty span skips this check.</param>
		public DecompressionVerification(int expectedDecodedLength, ReadOnlySpan<byte> expectedDecodedSha256,
			ReadOnlySpan<byte> expectedEncodedSha256 = default)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(expectedDecodedLength);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(expectedDecodedLength, Array.MaxLength);
			if (expectedDecodedSha256.Length != SHA256.HashSizeInBytes)
				throw new ArgumentException("The expected decoded SHA-256 digest must contain 32 bytes.", nameof(expectedDecodedSha256));
			if (!expectedEncodedSha256.IsEmpty && expectedEncodedSha256.Length != SHA256.HashSizeInBytes)
				throw new ArgumentException("The expected encoded SHA-256 digest must contain 32 bytes.", nameof(expectedEncodedSha256));

			ExpectedDecodedLength = expectedDecodedLength;
			_decodedSha256 = expectedDecodedSha256.ToArray();
			_encodedSha256 = expectedEncodedSha256.ToArray();
		}

		/// <summary>The exact decoded byte count to verify.</summary>
		public int ExpectedDecodedLength { get; }

		internal ReadOnlySpan<byte> DecodedSha256 => _decodedSha256;
		internal ReadOnlySpan<byte> EncodedSha256 => _encodedSha256;
	}
}
