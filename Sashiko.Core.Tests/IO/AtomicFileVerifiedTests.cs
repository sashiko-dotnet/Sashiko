using System.Security.Cryptography;
using Sashiko.Core.IO;

namespace Sashiko.Core.Tests.IO
{
	public sealed class AtomicFileVerifiedTests
	{
		private static readonly byte[] Original = [1, 2, 3];
		private static readonly byte[] Payload = [4, 5, 6, 7, 8];

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task WriteVerified_ShortNonSeekableReads_ReplaceOnlyAfterInputIsComplete(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			File.WriteAllBytes(path, Original);
			var bytes = Enumerable.Range(0, 180_000).Select(i => (byte)i).ToArray();
			using var source = new ProbeReadStream(bytes,
				_ => Assert.Equal(Original, File.ReadAllBytes(path)), chunkSize: 4093);

			await Write(asynchronous, path, source, bytes.Length, SHA256.HashData(bytes), overwrite: true);

			Assert.Equal(bytes, File.ReadAllBytes(path));
			Assert.False(source.IsDisposed);
			Assert.Equal(bytes.Length, source.BytesRead);
			Assert.Equal([path], Directory.GetFiles(directory.Folder));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task WriteVerified_UsesCurrentSourcePosition(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			using var source = new MemoryStream([0, 0, .. Payload]);
			source.Position = 2;

			await Write(asynchronous, path, source, Payload.Length, SHA256.HashData(Payload));

			Assert.Equal(Payload, File.ReadAllBytes(path));
			Assert.True(source.CanRead);
			Assert.Equal(source.Length, source.Position);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task WriteVerified_EmptyInputAndExpectedDigest_PublishAnEmptyFile(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			using var source = new ProbeReadStream([]);

			await Write(asynchronous, path, source, 0, SHA256.HashData(Array.Empty<byte>()));

			Assert.Empty(File.ReadAllBytes(path));
			Assert.False(source.IsDisposed);
			Assert.Equal([path], Directory.GetFiles(directory.Folder));
		}

		[Theory]
		[InlineData(false, false, "short")]
		[InlineData(false, true, "short")]
		[InlineData(true, false, "short")]
		[InlineData(true, true, "short")]
		[InlineData(false, false, "long")]
		[InlineData(false, true, "long")]
		[InlineData(true, false, "long")]
		[InlineData(true, true, "long")]
		[InlineData(false, false, "hash")]
		[InlineData(false, true, "hash")]
		[InlineData(true, false, "hash")]
		[InlineData(true, true, "hash")]
		public async Task WriteVerified_Mismatch_CleansOnlyOwnedOutput(
			bool asynchronous, bool existingDestination, string mismatch)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			var unrelated = directory.FilePath(".sashiko-unrelated.tmp");
			File.WriteAllBytes(unrelated, Original);
			if (existingDestination)
				File.WriteAllBytes(path, Original);
			using var source = new ProbeReadStream(Payload);
			var length = mismatch switch { "short" => Payload.Length + 1, "long" => Payload.Length - 1, _ => Payload.Length };
			var hash = SHA256.HashData(mismatch == "hash" ? Original : Payload);

			await Assert.ThrowsAsync<InvalidDataException>(() =>
				Write(asynchronous, path, source, length, hash, overwrite: true));

			Assert.False(source.IsDisposed);
			Assert.Equal(Original, File.ReadAllBytes(unrelated));
			Assert.Equal(existingDestination ? 2 : 1, Directory.GetFiles(directory.Folder).Length);
			if (existingDestination)
				Assert.Equal(Original, File.ReadAllBytes(path));
			else
				Assert.False(File.Exists(path));
		}

		[Theory]
		[InlineData(false, 0)]
		[InlineData(true, 0)]
		[InlineData(false, 3)]
		[InlineData(true, 3)]
		public async Task WriteVerified_OversizedInput_ConsumesOnlyOneExtraByte(bool asynchronous, int expectedLength)
		{
			using var directory = new TemporaryDirectory();
			using var source = new ProbeReadStream(new byte[200_000], chunkSize: 200_000);

			await Assert.ThrowsAsync<InvalidDataException>(() => Write(
				asynchronous, directory.FilePath(), source, expectedLength, SHA256.HashData(new byte[expectedLength])));

			Assert.Equal(expectedLength + 1, source.BytesRead);
			Assert.False(source.IsDisposed);
			Assert.Empty(Directory.GetFiles(directory.Folder));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task WriteVerified_MaximumLongExpectation_RejectsShortInputWithoutOverflow(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			using var source = new ProbeReadStream(Payload);

			await Assert.ThrowsAsync<InvalidDataException>(() =>
				Write(asynchronous, directory.FilePath(), source, long.MaxValue, SHA256.HashData(Payload)));

			Assert.Equal(Payload.Length, source.BytesRead);
			Assert.Empty(Directory.GetFiles(directory.Folder));
		}

		[Theory]
		[InlineData(false, 0)]
		[InlineData(true, 0)]
		[InlineData(false, 31)]
		[InlineData(true, 31)]
		[InlineData(false, 33)]
		[InlineData(true, 33)]
		public async Task WriteVerified_InvalidDigest_DoesNotReadOrCreateFiles(bool asynchronous, int hashLength)
		{
			using var directory = new TemporaryDirectory();
			using var source = new ProbeReadStream(Payload);

			var error = await Assert.ThrowsAsync<ArgumentException>(() =>
				Write(asynchronous, directory.FilePath(), source, Payload.Length, new byte[hashLength]));

			Assert.Equal("expectedSha256", error.ParamName);
			Assert.Equal(0, source.BytesRead);
			Assert.Empty(Directory.GetFiles(directory.Folder));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task WriteVerified_NegativeLength_DoesNotReadOrCreateFiles(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			using var source = new ProbeReadStream(Payload);

			var error = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
				Write(asynchronous, directory.FilePath(), source, -1, SHA256.HashData(Payload)));

			Assert.Equal("expectedLength", error.ParamName);
			Assert.Equal(0, source.BytesRead);
			Assert.Empty(Directory.GetFiles(directory.Folder));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task WriteVerified_RejectsInvalidSourcesAndPathsBeforeReading(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			var hash = SHA256.HashData(Payload);
			using var source = new ProbeReadStream(Payload);
			using var unreadable = new MemoryStream(Payload);
			unreadable.Dispose();

			await Assert.ThrowsAsync<ArgumentNullException>(() => Write(asynchronous, path, null!, Payload.Length, hash));
			await Assert.ThrowsAsync<ArgumentException>(() => Write(asynchronous, path, unreadable, Payload.Length, hash));
			await Assert.ThrowsAsync<ArgumentException>(() => Write(asynchronous, " ", source, Payload.Length, hash));
			await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
				Write(asynchronous, Path.Combine(directory.Folder, "missing", "data"), source, Payload.Length, hash));

			Assert.Equal(0, source.BytesRead);
			Assert.Empty(Directory.GetFileSystemEntries(directory.Folder));
		}

		[Theory]
		[InlineData(false, false)]
		[InlineData(true, false)]
		[InlineData(false, true)]
		[InlineData(true, true)]
		public async Task WriteVerified_ChangedDigestBuffer_DoesNotChangeTheExpectedHash(bool asynchronous, bool initiallyValid)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			var digest = initiallyValid ? SHA256.HashData(Payload) : new byte[32];
			using var source = new ProbeReadStream(Payload, count =>
			{
				if (count == 0)
				{
					if (initiallyValid)
						Array.Clear(digest);
					else
						SHA256.HashData(Payload).CopyTo(digest, 0);
				}
			});

			if (initiallyValid)
			{
				await Write(asynchronous, path, source, Payload.Length, digest);
				Assert.Equal(Payload, File.ReadAllBytes(path));
			}
			else
			{
				await Assert.ThrowsAsync<InvalidDataException>(() => Write(asynchronous, path, source, Payload.Length, digest));
				Assert.Empty(Directory.GetFiles(directory.Folder));
			}
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task WriteVerified_SourceFailure_PreservesCauseAndExistingDestination(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			File.WriteAllBytes(path, Original);
			var failure = new IOException("Synthetic read failure after partial output.");
			using var source = new ProbeReadStream(Payload, count =>
			{
				if (count > 0)
					throw failure;
			});

			var error = await Assert.ThrowsAsync<IOException>(() =>
				Write(asynchronous, path, source, Payload.Length, SHA256.HashData(Payload), overwrite: true));

			Assert.Same(failure, error);
			Assert.False(source.IsDisposed);
			Assert.Equal(Original, File.ReadAllBytes(path));
			Assert.Equal([path], Directory.GetFiles(directory.Folder));
		}

		[Theory]
		[InlineData(false, false)]
		[InlineData(true, false)]
		[InlineData(false, true)]
		[InlineData(true, true)]
		public async Task WriteVerified_WithoutOverwrite_PreservesDestinationEvenIfItAppearsDuringCopy(
			bool asynchronous, bool appearsDuringCopy)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			if (!appearsDuringCopy)
				File.WriteAllBytes(path, Original);
			using var source = new ProbeReadStream(Payload, count =>
			{
				if (appearsDuringCopy && count == 0)
					File.WriteAllBytes(path, Original);
			});

			await Assert.ThrowsAsync<IOException>(() =>
				Write(asynchronous, path, source, Payload.Length, SHA256.HashData(Payload)));

			Assert.Equal(Original, File.ReadAllBytes(path));
			Assert.Equal([path], Directory.GetFiles(directory.Folder));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task WriteVerified_DestinationBecomesDirectory_PreservesUnrelatedData(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			var unrelated = Path.Combine(path, "keep");
			using var source = new ProbeReadStream(Payload, count =>
			{
				if (count == 0)
				{
					Directory.CreateDirectory(path);
					File.WriteAllBytes(unrelated, Original);
				}
			});

			await Assert.ThrowsAsync<IOException>(() =>
				Write(asynchronous, path, source, Payload.Length, SHA256.HashData(Payload), overwrite: true));

			Assert.Equal(Original, File.ReadAllBytes(unrelated));
			Assert.Empty(Directory.GetFiles(directory.Folder));
		}

		[Theory]
		[InlineData(-1)]
		[InlineData(2)]
		[InlineData(5)]
		public async Task WriteVerifiedAsync_Cancellation_PreservesDestinationAndLeavesSourceOpen(int cancelAfter)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			File.WriteAllBytes(path, Original);
			using var cancellation = new CancellationTokenSource();
			if (cancelAfter < 0)
				cancellation.Cancel();
			using var source = new ProbeReadStream(Payload, count =>
			{
				if (count == cancelAfter)
					cancellation.Cancel();
			});

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AtomicFile.WriteVerifiedAsync(
				path, source, Payload.Length, SHA256.HashData(Payload), overwrite: true, cancellation.Token));

			Assert.False(source.IsDisposed);
			if (cancelAfter < 0)
				Assert.Equal(0, source.BytesRead);
			Assert.Equal(Original, File.ReadAllBytes(path));
			Assert.Equal([path], Directory.GetFiles(directory.Folder));
		}

		[UnixFact]
		public async Task WriteVerified_RejectsExistingAndIntroducedSymbolicLinks()
		{
			using var directory = new TemporaryDirectory();
			var target = directory.FilePath("target");
			File.WriteAllBytes(target, Original);
			foreach (var asynchronous in new[] { false, true })
			{
				foreach (var introducedDuringCopy in new[] { false, true })
				{
					var link = directory.FilePath($"link-{asynchronous}-{introducedDuringCopy}");
					if (!introducedDuringCopy)
						File.CreateSymbolicLink(link, target);
					using var source = new ProbeReadStream(Payload, count =>
					{
						if (introducedDuringCopy && count == 0)
							File.CreateSymbolicLink(link, target);
					});

					await Assert.ThrowsAsync<IOException>(() =>
						Write(asynchronous, link, source, Payload.Length, SHA256.HashData(Payload), overwrite: true));

					Assert.NotNull(new FileInfo(link).LinkTarget);
					Assert.Equal(Original, File.ReadAllBytes(target));
				}
			}
			Assert.Empty(Directory.GetFiles(directory.Folder, ".sashiko-*.tmp"));
		}

		private static Task Write(
			bool asynchronous, string path, Stream source, long expectedLength, byte[] hash, bool overwrite = false)
		{
			if (asynchronous)
				return AtomicFile.WriteVerifiedAsync(path, source, expectedLength, hash, overwrite);
			AtomicFile.WriteVerified(path, source, expectedLength, hash, overwrite);
			return Task.CompletedTask;
		}
	}
}
