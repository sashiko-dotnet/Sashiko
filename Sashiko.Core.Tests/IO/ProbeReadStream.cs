namespace Sashiko.Core.Tests.IO
{
	// Deliberately non-seekable: readers must handle short reads and must not inspect Length/Position.
	internal sealed class ProbeReadStream(byte[] bytes, Action<int>? beforeRead = null, int chunkSize = 2) : Stream
	{
		public int BytesRead { get; private set; }
		public bool IsDisposed { get; private set; }
		public override bool CanRead => !IsDisposed;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

		public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

		public override int Read(Span<byte> buffer)
		{
			ObjectDisposedException.ThrowIf(IsDisposed, this);
			beforeRead?.Invoke(BytesRead);
			var count = Math.Min(Math.Min(buffer.Length, chunkSize), bytes.Length - BytesRead);
			bytes.AsSpan(BytesRead, count).CopyTo(buffer);
			BytesRead += count;
			return count;
		}

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return ValueTask.FromResult(Read(buffer.Span));
		}

		protected override void Dispose(bool disposing)
		{
			IsDisposed = true;
			base.Dispose(disposing);
		}

		public override void Flush() => throw new NotSupportedException();
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}
}
