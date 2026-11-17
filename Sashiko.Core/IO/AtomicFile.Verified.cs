using System.Security.Cryptography;

namespace Sashiko.Core.IO
{
	public static partial class AtomicFile
	{
		// Match the conventional Stream.CopyTo buffer; independent of the expected payload size.
		private const int VerifiedCopyBufferSize = 80 * 1024;

		/// <summary>Stages a source stream and verifies the staged length and SHA-256 before publishing it.</summary>
		/// <param name="path">Destination in an existing application-controlled directory.</param>
		/// <param name="source">Readable stream, consumed from its current position and left open on every outcome.</param>
		/// <param name="expectedLength">Exact expected byte count, which must be nonnegative. At most one extra byte is consumed to check EOF.</param>
		/// <param name="expectedSha256">Trusted expected digest, exactly 32 bytes. Snapshotted before reading the source.</param>
		/// <param name="overwrite">Whether an existing destination may be replaced. Coordinate concurrent writers even when false.</param>
		/// <exception cref="InvalidDataException">The input length or staged SHA-256 differs from the expected value.</exception>
		/// <remarks>
		/// Uses the same staging, metadata, cleanup and publication contract as Write. Verification rereads
		/// the staged file through its open handle; it is not a physical-media or power-loss durability check.
		/// The source position is not restored. Do not use a destination that aliases the source file.
		/// A digest is only as trustworthy as the caller's expected value; this does not authenticate a manifest.
		/// </remarks>
		public static void WriteVerified(
			string path,
			Stream source,
			long expectedLength,
			ReadOnlySpan<byte> expectedSha256,
			bool overwrite = false)
		{
			ValidateSource(source);
			var expectedHash = GetExpectedHash(expectedLength, expectedSha256);
			var destination = GetDestination(path);
			using var staged = new StagedFile(destination, asynchronous: false, readable: true);
			CopyExpectedLength(source, staged.Stream, expectedLength);
			staged.Stream.Flush();
			RewindForVerification(staged.Stream, expectedLength);
			EnsureHashMatches(SHA256.HashData(staged.Stream), expectedHash);
			staged.Commit(destination, overwrite);
		}

		/// <summary>Asynchronously stages and verifies a source stream before publishing it, leaving the source open.</summary>
		/// <param name="path">Destination in an existing application-controlled directory.</param>
		/// <param name="source">Readable stream, consumed from its current position without requiring seeking.</param>
		/// <param name="expectedLength">Exact nonnegative byte count. At most one extra byte is consumed to check EOF.</param>
		/// <param name="expectedSha256">Trusted 32-byte SHA-256 digest, snapshotted before the first await.</param>
		/// <param name="overwrite">Whether an existing destination may be replaced. Coordinate concurrent writers.</param>
		/// <param name="cancellationToken">Cancellation while copying or verifying, and immediately before commit.</param>
		/// <exception cref="InvalidDataException">The input length or staged SHA-256 differs from the expected value.</exception>
		/// <remarks>
		/// Uses the WriteVerified contract. Cancellation does not interrupt the final synchronous
		/// flush/close/move section or change a successful commit into a canceled task. Pending source
		/// reads depend on the source's cancellation support. The source is never disposed or rewound.
		/// </remarks>
		public static async Task WriteVerifiedAsync(
			string path,
			Stream source,
			long expectedLength,
			ReadOnlyMemory<byte> expectedSha256,
			bool overwrite = false,
			CancellationToken cancellationToken = default)
		{
			ValidateSource(source);
			var expectedHash = GetExpectedHash(expectedLength, expectedSha256.Span);
			var destination = GetDestination(path);
			cancellationToken.ThrowIfCancellationRequested();
			using var staged = new StagedFile(destination, asynchronous: true, readable: true);
			await CopyExpectedLengthAsync(source, staged.Stream, expectedLength, cancellationToken).ConfigureAwait(false);
			await staged.Stream.FlushAsync(cancellationToken).ConfigureAwait(false);
			RewindForVerification(staged.Stream, expectedLength);
			var actualHash = await SHA256.HashDataAsync(staged.Stream, cancellationToken).ConfigureAwait(false);
			EnsureHashMatches(actualHash, expectedHash);
			cancellationToken.ThrowIfCancellationRequested();
			staged.Commit(destination, overwrite);
		}

		private static byte[] GetExpectedHash(long expectedLength, ReadOnlySpan<byte> expectedSha256)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(expectedLength);
			if (expectedSha256.Length != SHA256.HashSizeInBytes)
				throw new ArgumentException("The expected SHA-256 digest must contain exactly 32 bytes.", nameof(expectedSha256));
			return expectedSha256.ToArray();
		}

		private static void CopyExpectedLength(Stream source, Stream output, long expectedLength)
		{
			var buffer = CreateVerificationBuffer(expectedLength);
			var remaining = expectedLength;
			while (remaining > 0)
			{
				var read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
				EnsureNotTruncated(read);
				output.Write(buffer, 0, read);
				remaining -= read;
			}

			// A separate one-byte probe avoids expectedLength + 1 overflowing at long.MaxValue.
			EnsureEndOfInput(source.Read(buffer, 0, 1));
		}

		private static async Task CopyExpectedLengthAsync(
			Stream source, Stream output, long expectedLength, CancellationToken cancellationToken)
		{
			var buffer = CreateVerificationBuffer(expectedLength);
			var remaining = expectedLength;
			while (remaining > 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var read = await source.ReadAsync(
					buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), cancellationToken).ConfigureAwait(false);
				cancellationToken.ThrowIfCancellationRequested();
				EnsureNotTruncated(read);
				await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
				remaining -= read;
			}

			cancellationToken.ThrowIfCancellationRequested();
			var extra = await source.ReadAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			EnsureEndOfInput(extra);
		}

		private static byte[] CreateVerificationBuffer(long expectedLength)
			=> new byte[(int)Math.Max(1, Math.Min(VerifiedCopyBufferSize, expectedLength))];

		private static void EnsureNotTruncated(int read)
		{
			if (read == 0)
				throw new InvalidDataException("The input is shorter than the expected byte count.");
		}

		private static void EnsureEndOfInput(int extra)
		{
			if (extra != 0)
				throw new InvalidDataException("The input exceeds the expected byte count.");
		}

		private static void RewindForVerification(FileStream staged, long expectedLength)
		{
			if (staged.Length != expectedLength)
				throw new InvalidDataException("The staged file length differs from the expected byte count.");
			staged.Position = 0;
		}

		private static void EnsureHashMatches(ReadOnlySpan<byte> actual, ReadOnlySpan<byte> expected)
		{
			if (!CryptographicOperations.FixedTimeEquals(actual, expected))
				throw new InvalidDataException("The staged file does not match the expected SHA-256 digest.");
		}
	}
}
