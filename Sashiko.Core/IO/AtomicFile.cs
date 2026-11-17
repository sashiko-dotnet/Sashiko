namespace Sashiko.Core.IO
{
	/// <summary>Stages complete output beside its destination before publishing it with <see cref="File.Move(string, string, bool)"/>.</summary>
	/// <remarks>
	/// Use an application-controlled directory and coordinate writers. This is not a filesystem
	/// transaction or a power-loss durability guarantee. Replacement uses the temporary file's
	/// metadata; destination permissions and ACLs are not preserved. See the Core README for the full contract.
	/// </remarks>
	public static class AtomicFile
	{
		/// <summary>Writes bytes to a temporary file, flushes it to disk, closes it, and moves it to <paramref name="path"/>.</summary>
		/// <param name="path">Destination in an existing directory.</param>
		/// <param name="bytes">Complete output to publish.</param>
		/// <param name="overwrite">Whether an existing destination may be replaced. Coordinate concurrent writers even when false.</param>
		public static void Write(string path, ReadOnlySpan<byte> bytes, bool overwrite = false)
		{
			var destination = GetDestination(path);
			using var staged = new StagedFile(destination, asynchronous: false);
			staged.Stream.Write(bytes);
			staged.Commit(destination, overwrite);
		}

		/// <summary>Copies from the source's current position to EOF before publishing the output. Leaves the source open.</summary>
		public static void Write(string path, Stream source, bool overwrite = false)
		{
			ValidateSource(source);
			var destination = GetDestination(path);
			using var staged = new StagedFile(destination, asynchronous: false);
			source.CopyTo(staged.Stream);
			staged.Commit(destination, overwrite);
		}

		/// <summary>Asynchronously stages bytes, then flushes and commits the file.</summary>
		/// <remarks>
		/// Cancellation is checked immediately before the synchronous disk flush/commit section.
		/// Once that section begins, cancellation does not change a successful commit into a canceled task.
		/// The caller must keep the supplied memory unchanged until completion.
		/// </remarks>
		public static async Task WriteAsync(
			string path,
			ReadOnlyMemory<byte> bytes,
			bool overwrite = false,
			CancellationToken cancellationToken = default)
		{
			var destination = GetDestination(path);
			cancellationToken.ThrowIfCancellationRequested();
			using var staged = new StagedFile(destination, asynchronous: true);
			await staged.Stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			staged.Commit(destination, overwrite);
		}

		/// <summary>Asynchronously copies the source to EOF and commits the output. Leaves the source open.</summary>
		/// <remarks>Cancellation is honored before the synchronous disk flush/commit section, as in the byte overload.</remarks>
		public static async Task WriteAsync(
			string path,
			Stream source,
			bool overwrite = false,
			CancellationToken cancellationToken = default)
		{
			ValidateSource(source);
			var destination = GetDestination(path);
			cancellationToken.ThrowIfCancellationRequested();
			using var staged = new StagedFile(destination, asynchronous: true);
			await source.CopyToAsync(staged.Stream, cancellationToken).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			staged.Commit(destination, overwrite);
		}

		private static string GetDestination(string path)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(path);
			var destination = Path.GetFullPath(path);
			EnsureRegularDestination(destination);
			return destination;
		}

		private static void EnsureRegularDestination(string path)
		{
			try
			{
				var attributes = File.GetAttributes(path);
				if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
					throw new IOException("The destination must be a regular file or an unused filename.");
			}
			catch (FileNotFoundException) { /* A new destination is allowed. */ }
		}

		private static void ValidateSource(Stream source)
		{
			ArgumentNullException.ThrowIfNull(source);
			if (!source.CanRead)
				throw new ArgumentException("The source stream must be readable.", nameof(source));
		}

		private sealed class StagedFile : IDisposable
		{
			private readonly string _path;
			private bool _committed;
			public FileStream Stream { get; }

			public StagedFile(string destination, bool asynchronous)
			{
				_path = Path.Combine(Path.GetDirectoryName(destination)!, $".sashiko-{Guid.NewGuid():N}.tmp");
				var options = new FileStreamOptions
				{
					Mode = FileMode.CreateNew,
					Access = FileAccess.Write,
					Share = FileShare.None,
					Options = asynchronous ? FileOptions.Asynchronous : FileOptions.None
				};
				if (!OperatingSystem.IsWindows())
					options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

				// Ownership begins only after CreateNew succeeds. A name collision is never cleaned up.
				Stream = new FileStream(_path, options);
			}

			public void Commit(string destination, bool overwrite)
			{
				Stream.Flush(flushToDisk: true);
				Stream.Dispose();
				EnsureRegularDestination(destination);
				File.Move(_path, destination, overwrite);
				_committed = true;
			}

			public void Dispose()
			{
				// Cleanup must not hide an earlier write/flush/commit error. Never remove the destination.
				try { Stream.Dispose(); }
				catch (IOException) { }
				catch (UnauthorizedAccessException) { }

				if (_committed)
					return;

				try { File.Delete(_path); }
				catch (IOException) { }
				catch (UnauthorizedAccessException) { }
			}
		}
	}
}
