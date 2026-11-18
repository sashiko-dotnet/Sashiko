namespace Sashiko.Core.Compression
{
	// Enforces the encoded-output limit even when codec finalization emits headers/trailers.
	internal sealed class LimitedBufferStream(int maxBytes) : Stream
	{
		private readonly MemoryStream _buffer = new();
		public byte[] ToArray() => _buffer.ToArray();
		public override bool CanRead => false;
		public override bool CanSeek => false;
		public override bool CanWrite => _buffer.CanWrite;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

		public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			if (buffer.Length > maxBytes - _buffer.Length)
				throw new InvalidDataException("The compressed output exceeds the configured byte limit.");
			_buffer.Write(buffer);
		}

		public override void WriteByte(byte value)
		{
			Span<byte> single = stackalloc byte[1];
			single[0] = value;
			Write(single);
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Write(buffer.Span);
			return ValueTask.CompletedTask;
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			=> WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

		public override void Flush() => _buffer.Flush();
		public override Task FlushAsync(CancellationToken cancellationToken) => _buffer.FlushAsync(cancellationToken);

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				_buffer.Dispose();
			base.Dispose(disposing);
		}

		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
	}
}
