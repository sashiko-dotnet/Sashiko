using Sashiko.Core.IO;

namespace Sashiko.Core.Tests.IO
{
	public sealed class AtomicFileTests
	{
		private static readonly byte[] Original = [1, 2, 3];
		private static readonly byte[] Replacement = [4, 5, 6, 7, 8];

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task Write_CreatesCompleteOutput(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();

			if (asynchronous)
				await AtomicFile.WriteAsync(path, Replacement);
			else
				AtomicFile.Write(path, Replacement);

			Assert.Equal(Replacement, File.ReadAllBytes(path));
			Assert.Equal([path], Directory.GetFiles(directory.Folder));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task Write_ReplacesOnlyAfterSourceReachesEof_AndLeavesSourceOpen(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			File.WriteAllBytes(path, Original);
			using var source = new ProbeReadStream(Replacement, _ => Assert.Equal(Original, File.ReadAllBytes(path)));

			if (asynchronous)
				await AtomicFile.WriteAsync(path, source, overwrite: true);
			else
				AtomicFile.Write(path, source, overwrite: true);

			Assert.Equal(Replacement, File.ReadAllBytes(path));
			Assert.False(source.IsDisposed);
			Assert.Equal([path], Directory.GetFiles(directory.Folder));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task Write_WithoutOverwrite_PreservesExistingDestination(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			File.WriteAllBytes(path, Original);

			if (asynchronous)
				await Assert.ThrowsAsync<IOException>(() => AtomicFile.WriteAsync(path, Replacement));
			else
				Assert.Throws<IOException>(() => AtomicFile.Write(path, Replacement));

			Assert.Equal(Original, File.ReadAllBytes(path));
			Assert.Equal([path], Directory.GetFiles(directory.Folder));
		}

		[Theory]
		[InlineData(false, false)]
		[InlineData(false, true)]
		[InlineData(true, false)]
		[InlineData(true, true)]
		public async Task Write_SourceFailure_PreservesDestinationAndUnrelatedFiles(bool asynchronous, bool existingDestination)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			var unrelated = directory.FilePath(".sashiko-unrelated.tmp");
			File.WriteAllBytes(unrelated, Original);
			if (existingDestination)
				File.WriteAllBytes(path, Original);
			var failure = new IOException("Synthetic read failure after partial output.");
			using var source = new ProbeReadStream(Replacement, count =>
			{
				if (count > 0)
					throw failure;
			});

			var error = asynchronous
				? await Assert.ThrowsAsync<IOException>(() => AtomicFile.WriteAsync(path, source, overwrite: true))
				: Assert.Throws<IOException>(() => AtomicFile.Write(path, source, overwrite: true));

			Assert.Same(failure, error);
			Assert.False(source.IsDisposed);
			Assert.Equal(Original, File.ReadAllBytes(unrelated));
			Assert.Equal(existingDestination ? 2 : 1, Directory.GetFiles(directory.Folder).Length);
			if (existingDestination)
				Assert.Equal(Original, File.ReadAllBytes(path));
			else
				Assert.False(File.Exists(path));
		}

		[Fact]
		public async Task WriteAsync_PreCanceled_DoesNotTouchDestination()
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			File.WriteAllBytes(path, Original);
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
				AtomicFile.WriteAsync(path, Replacement, overwrite: true, cancellation.Token));

			Assert.Equal(Original, File.ReadAllBytes(path));
			Assert.Equal([path], Directory.GetFiles(directory.Folder));
		}

		[Fact]
		public async Task WriteAsync_CanceledAtEof_DoesNotCommit()
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			File.WriteAllBytes(path, Original);
			using var cancellation = new CancellationTokenSource();
			using var source = new ProbeReadStream(Replacement, count =>
			{
				if (count == Replacement.Length)
					cancellation.Cancel();
			});

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
				AtomicFile.WriteAsync(path, source, overwrite: true, cancellation.Token));

			Assert.Equal(Original, File.ReadAllBytes(path));
			Assert.False(source.IsDisposed);
			Assert.Equal([path], Directory.GetFiles(directory.Folder));
		}

		[Fact]
		public void Write_DoesNotCreateMissingParentDirectories()
		{
			using var directory = new TemporaryDirectory();
			var missing = directory.FilePath("missing");

			Assert.Throws<DirectoryNotFoundException>(() => AtomicFile.Write(Path.Combine(missing, "data"), Replacement));
			Assert.False(Directory.Exists(missing));
			Assert.Empty(Directory.GetFileSystemEntries(directory.Folder));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task Write_EmptyPayload_PublishesAnEmptyFile(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			if (asynchronous)
				await AtomicFile.WriteAsync(path, ReadOnlyMemory<byte>.Empty);
			else
				AtomicFile.Write(path, ReadOnlySpan<byte>.Empty);
			Assert.Empty(File.ReadAllBytes(path));
			Assert.Equal([path], Directory.GetFiles(directory.Folder));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task Write_DestinationBecomesDirectory_DoesNotDeleteItOrUnrelatedData(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			var unrelated = Path.Combine(path, "keep");
			using var source = new ProbeReadStream(Replacement, count =>
			{
				if (count == 0)
				{
					Directory.CreateDirectory(path);
					File.WriteAllBytes(unrelated, Original);
				}
			});

			if (asynchronous)
				await Assert.ThrowsAsync<IOException>(() => AtomicFile.WriteAsync(path, source, overwrite: true));
			else
				Assert.Throws<IOException>(() => AtomicFile.Write(path, source, overwrite: true));

			Assert.Equal(Original, File.ReadAllBytes(unrelated));
			Assert.Empty(Directory.GetFiles(directory.Folder));
		}

		[Fact]
		public async Task Write_InvalidSourcesAndPaths_DoNotCreateFiles()
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			using var unreadable = new MemoryStream(Replacement);
			unreadable.Dispose();

			Assert.Throws<ArgumentNullException>(() => AtomicFile.Write(path, (Stream)null!));
			Assert.Throws<ArgumentException>(() => AtomicFile.Write(path, unreadable));
			Assert.Throws<ArgumentException>(() => AtomicFile.Write(" ", Replacement));
			await Assert.ThrowsAsync<ArgumentNullException>(() => AtomicFile.WriteAsync(path, (Stream)null!));
			await Assert.ThrowsAsync<ArgumentException>(() => AtomicFile.WriteAsync(path, unreadable));
			await Assert.ThrowsAsync<ArgumentException>(() => AtomicFile.WriteAsync(" ", Replacement));

			Assert.Empty(Directory.GetFiles(directory.Folder));
		}

		[Fact]
		public void Write_DestinationAppearsDuringStaging_WithoutOverwrite_PreservesIt()
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			using var source = new ProbeReadStream(Replacement, count =>
			{
				if (count == 0)
					File.WriteAllBytes(path, Original);
			});

			Assert.Throws<IOException>(() => AtomicFile.Write(path, source));
			Assert.Equal(Original, File.ReadAllBytes(path));
			Assert.Equal([path], Directory.GetFiles(directory.Folder));
		}

		[UnixFact]
		public void Write_RejectsSymbolicLinkDestinations_WithoutTouchingTarget()
		{
			using var directory = new TemporaryDirectory();
			var target = directory.FilePath("target");
			var link = directory.FilePath("link");
			File.WriteAllBytes(target, Original);
			File.CreateSymbolicLink(link, target);

			Assert.Throws<IOException>(() => AtomicFile.Write(link, Replacement, overwrite: true));
			Assert.Equal(Original, File.ReadAllBytes(target));
			Assert.NotNull(new FileInfo(link).LinkTarget);
		}

		[UnixFact]
		public void Write_RejectsDanglingSymbolicLink_WithoutCreatingItsTarget()
		{
			using var directory = new TemporaryDirectory();
			var target = directory.FilePath("absent");
			var link = directory.FilePath("link");
			File.CreateSymbolicLink(link, target);

			Assert.Throws<IOException>(() => AtomicFile.Write(link, Replacement, overwrite: true));
			Assert.False(File.Exists(target));
			Assert.NotNull(new FileInfo(link).LinkTarget);
		}

		[UnixFact]
		public void Write_RejectsLinkIntroducedDuringStaging_AndCleansUpItsOwnOutput()
		{
			using var directory = new TemporaryDirectory();
			var target = directory.FilePath("target");
			var link = directory.FilePath("link");
			File.WriteAllBytes(target, Original);
			using var source = new ProbeReadStream(Replacement, count =>
			{
				if (count == 0)
					File.CreateSymbolicLink(link, target);
			});

			Assert.Throws<IOException>(() => AtomicFile.Write(link, source, overwrite: true));
			Assert.Equal(Original, File.ReadAllBytes(target));
			Assert.Equal(2, Directory.GetFiles(directory.Folder).Length);
		}

		[UnixFact]
		public void Write_StagingAndPublishedFileHaveNoGroupOrOtherPermissions()
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			using var source = new ProbeReadStream(Replacement, _ =>
			{
				var temporary = Assert.Single(Directory.GetFiles(directory.Folder));
				AssertPrivatePermissions(temporary);
			});

			AtomicFile.Write(path, source);
			AssertPrivatePermissions(path);
		}

		private static void AssertPrivatePermissions(string path)
		{
			if (OperatingSystem.IsWindows())
				throw new PlatformNotSupportedException();
			var mode = File.GetUnixFileMode(path);
			var publicPermissions = UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
				UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;
			Assert.Equal((UnixFileMode)0, mode & publicPermissions);
		}
	}
}
