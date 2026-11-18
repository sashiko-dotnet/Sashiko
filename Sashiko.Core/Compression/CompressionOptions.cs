using System.IO.Compression;

namespace Sashiko.Core.Compression
{
	/// <summary>Optional compression effort. Does not affect the format required for decoding.</summary>
	public sealed class CompressionOptions
	{
		/// <summary>A runtime speed/size preference. Null selects Optimal unless BrotliQuality is supplied.</summary>
		public CompressionLevel? Level { get; init; }

		/// <summary>Explicit Brotli quality from 0 to 11. Cannot be combined with Level or used with GZip.</summary>
		public int? BrotliQuality { get; init; }
	}
}
