namespace Sashiko.Core.IO
{
	/// <summary>Reads complete byte payloads while enforcing a limit on bytes actually consumed.</summary>
	/// <remarks>
	/// Limits are inclusive. Oversized input throws <see cref="InvalidDataException"/> without returning
	/// a partial payload. Stream overloads leave the source open and may consume one byte beyond the limit.
	/// The byte limit is not a total allocation, CPU-time, or decompression limit.
	/// </remarks>
	public static class BoundedRead
	{
		// Match Stream.CopyTo's conventional 80 KiB default. This is not the payload limit.
		private const int BufferSize = 80 * 1024;

		/// <summary>Reads a file to EOF, enforcing <paramref name="maxBytes"/> without trusting its reported length.</summary>
		public static byte[] ReadAllBytes(string path, int maxBytes)
		{
			ValidateLimit(maxBytes);
			using var source = File.OpenRead(path);
			return ReadAllBytes(source, maxBytes);
		}

		/// <summary>Reads from the stream's current position to EOF. Does not require seeking or a reported length.</summary>
		public static byte[] ReadAllBytes(Stream source, int maxBytes)
		{
			ValidateSource(source, maxBytes);
			using var output = new MemoryStream();
			var buffer = new byte[Math.Min(BufferSize, maxBytes + 1)];
			while (true)
			{
				var remaining = maxBytes - (int)output.Length;
				var read = source.Read(buffer, 0, Math.Min(buffer.Length, remaining + 1));
				if (read == 0)
					return output.ToArray();
				EnsureWithinLimit(read, remaining);
				output.Write(buffer, 0, read);
			}
		}

		/// <summary>Asynchronously reads a file to EOF while enforcing an inclusive byte limit.</summary>
		public static async Task<byte[]> ReadAllBytesAsync(
			string path,
			int maxBytes,
			CancellationToken cancellationToken = default)
		{
			ValidateLimit(maxBytes);
			cancellationToken.ThrowIfCancellationRequested();
			using var source = new FileStream(path, new FileStreamOptions
			{
				Mode = FileMode.Open,
				Access = FileAccess.Read,
				Share = FileShare.Read,
				Options = FileOptions.Asynchronous | FileOptions.SequentialScan
			});
			return await ReadAllBytesAsync(source, maxBytes, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>Asynchronously reads from the stream's current position to EOF, leaving it open.</summary>
		public static async Task<byte[]> ReadAllBytesAsync(
			Stream source,
			int maxBytes,
			CancellationToken cancellationToken = default)
		{
			ValidateSource(source, maxBytes);
			using var output = new MemoryStream();
			var buffer = new byte[Math.Min(BufferSize, maxBytes + 1)];
			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var remaining = maxBytes - (int)output.Length;
				var read = await source.ReadAsync(
					buffer.AsMemory(0, Math.Min(buffer.Length, remaining + 1)), cancellationToken).ConfigureAwait(false);
				cancellationToken.ThrowIfCancellationRequested();
				if (read == 0)
					return output.ToArray();
				EnsureWithinLimit(read, remaining);
				await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
			}
		}

		private static void ValidateLimit(int maxBytes)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(maxBytes);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(maxBytes, Array.MaxLength);
		}

		private static void ValidateSource(Stream source, int maxBytes)
		{
			ArgumentNullException.ThrowIfNull(source);
			ValidateLimit(maxBytes);
			if (!source.CanRead)
				throw new ArgumentException("The source stream must be readable.", nameof(source));
		}

		private static void EnsureWithinLimit(int read, int remaining)
		{
			if (read > remaining)
				throw new InvalidDataException("The input exceeds the configured byte limit.");
		}
	}
}
