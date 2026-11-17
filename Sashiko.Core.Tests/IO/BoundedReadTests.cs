using Sashiko.Core.IO;

namespace Sashiko.Core.Tests.IO
{
	public sealed class BoundedReadTests
	{
		[Theory]
		[InlineData(false, 0)]
		[InlineData(true, 0)]
		[InlineData(false, 5)]
		[InlineData(true, 5)]
		[InlineData(false, 100000)]
		[InlineData(true, 100000)]
		public async Task ReadAllBytes_AcceptsExactLimit_AndLeavesNonSeekableSourceOpen(bool asynchronous, int length)
		{
			var bytes = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();
			using var source = new ProbeReadStream(bytes, chunkSize: 1000);

			var result = asynchronous
				? await BoundedRead.ReadAllBytesAsync(source, length)
				: BoundedRead.ReadAllBytes(source, length);

			Assert.Equal(bytes, result);
			Assert.False(source.IsDisposed);
		}

		[Theory]
		[InlineData(false, 0)]
		[InlineData(true, 0)]
		[InlineData(false, 5)]
		[InlineData(true, 5)]
		[InlineData(false, 100000)]
		[InlineData(true, 100000)]
		public async Task ReadAllBytes_OversizedInput_ConsumesOnlyLimitPlusOne_AndLeavesSourceOpen(bool asynchronous, int limit)
		{
			using var source = new ProbeReadStream(new byte[limit + 100], chunkSize: 1000);

			if (asynchronous)
				await Assert.ThrowsAsync<InvalidDataException>(() => BoundedRead.ReadAllBytesAsync(source, limit));
			else
				Assert.Throws<InvalidDataException>(() => BoundedRead.ReadAllBytes(source, limit));

			Assert.Equal(limit + 1, source.BytesRead);
			Assert.False(source.IsDisposed);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task ReadAllBytes_FileOverload_UsesActualBytes(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			byte[] bytes = [0, 255, 10, 13];
			File.WriteAllBytes(path, bytes);

			var result = asynchronous
				? await BoundedRead.ReadAllBytesAsync(path, bytes.Length)
				: BoundedRead.ReadAllBytes(path, bytes.Length);
			Assert.Equal(bytes, result);

			if (asynchronous)
				await Assert.ThrowsAsync<InvalidDataException>(() => BoundedRead.ReadAllBytesAsync(path, bytes.Length - 1));
			else
				Assert.Throws<InvalidDataException>(() => BoundedRead.ReadAllBytes(path, bytes.Length - 1));

			// Both success and failure close the internally opened file (including on Windows).
			using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task ReadAllBytes_GrowingFile_EnforcesActualConsumedBytes(bool asynchronous)
		{
			using var directory = new TemporaryDirectory();
			var path = directory.FilePath();
			File.WriteAllBytes(path, [1, 2]);
			using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			Assert.Equal(2, source.Length);
			using (var append = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
				append.Write([3, 4]);

			if (asynchronous)
				await Assert.ThrowsAsync<InvalidDataException>(() => BoundedRead.ReadAllBytesAsync(source, 2));
			else
				Assert.Throws<InvalidDataException>(() => BoundedRead.ReadAllBytes(source, 2));
		}

		[Fact]
		public void ReadAllBytes_StartsAtCurrentPosition()
		{
			using var source = new MemoryStream([0, 1, 2, 3]);
			source.Position = 2;
			Assert.Equal(new byte[] { 2, 3 }, BoundedRead.ReadAllBytes(source, 2));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task ReadAllBytes_SourceFailure_PreservesOriginalExceptionAndStream(bool asynchronous)
		{
			var failure = new IOException("Synthetic source failure.");
			using var source = new ProbeReadStream([1, 2, 3], count =>
			{
				if (count > 0)
					throw failure;
			});

			var error = asynchronous
				? await Assert.ThrowsAsync<IOException>(() => BoundedRead.ReadAllBytesAsync(source, 10))
				: Assert.Throws<IOException>(() => BoundedRead.ReadAllBytes(source, 10));

			Assert.Same(failure, error);
			Assert.False(source.IsDisposed);
		}

		[Theory]
		[InlineData(-1)]
		[InlineData(int.MaxValue)]
		public async Task ReadAllBytes_InvalidLimit_DoesNotConsumeSource(int limit)
		{
			using var source = new ProbeReadStream([1, 2, 3]);
			Assert.Throws<ArgumentOutOfRangeException>(() => BoundedRead.ReadAllBytes(source, limit));
			await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => BoundedRead.ReadAllBytesAsync(source, limit));
			Assert.Equal(0, source.BytesRead);
		}

		[Fact]
		public async Task ReadAllBytesAsync_CancellationBetweenReads_StopsConsumptionAndLeavesSourceOpen()
		{
			using var cancellation = new CancellationTokenSource();
			using var source = new ProbeReadStream([1, 2, 3], _ => cancellation.Cancel());

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
				BoundedRead.ReadAllBytesAsync(source, 10, cancellation.Token));

			Assert.Equal(2, source.BytesRead);
			Assert.False(source.IsDisposed);
		}

		[Fact]
		public async Task ReadAllBytesAsync_PreCanceled_DoesNotRead()
		{
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			using var source = new ProbeReadStream([1, 2, 3]);

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
				BoundedRead.ReadAllBytesAsync(source, 10, cancellation.Token));
			Assert.Equal(0, source.BytesRead);
		}

		[Fact]
		public async Task ReadAllBytes_InvalidSource_IsRejected()
		{
			using var unreadable = new MemoryStream();
			unreadable.Dispose();

			Assert.Throws<ArgumentNullException>(() => BoundedRead.ReadAllBytes((Stream)null!, 10));
			Assert.Throws<ArgumentException>(() => BoundedRead.ReadAllBytes(unreadable, 10));
			await Assert.ThrowsAsync<ArgumentNullException>(() => BoundedRead.ReadAllBytesAsync((Stream)null!, 10));
			await Assert.ThrowsAsync<ArgumentException>(() => BoundedRead.ReadAllBytesAsync(unreadable, 10));
		}
	}
}
