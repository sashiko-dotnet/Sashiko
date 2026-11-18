using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

// Exploratory codec-only comparison. Not a performance test or a Sashiko allocation benchmark.
var random = new byte[65_536];
new Random(42).NextBytes(random);
var fixtures = new (string Name, byte[] Bytes)[]
{
    ("small-json", Encoding.UTF8.GetBytes("{\"theme\":\"dark\",\"language\":\"en\",\"revision\":1}")),
    ("records-json", JsonSerializer.SerializeToUtf8Bytes(Enumerable.Range(0, 10_000).Select(i =>
        new { Id = i, Category = $"category-{i % 20}", Label = "synthetic record", Value = i % 1000 }))),
    ("random-bytes", random)
};
var settings = new (string Name, CompressionLevel? Level, int? Quality)[]
{
    ("gzip-fast", CompressionLevel.Fastest, null), ("gzip-optimal", CompressionLevel.Optimal, null),
    ("gzip-smallest", CompressionLevel.SmallestSize, null),
    ("brotli-q1", null, 1), ("brotli-q4", null, 4), ("brotli-q11", null, 11)
};
Console.WriteLine("fixture,input_bytes,setting,compressed_bytes,compress_ms,decompress_ms,managed_compress_alloc_bytes");
foreach (var fixture in fixtures)
{
    foreach (var setting in settings)
    {
        var compressed = Encode(fixture.Bytes, setting.Level, setting.Quality);
        if (!Decode(compressed, setting.Quality.HasValue).AsSpan().SequenceEqual(fixture.Bytes))
            throw new InvalidDataException("Codec round trip changed the fixture.");
        double encodeMs = 0, decodeMs = 0;
        long allocations = 0;
        for (var sample = 0; sample < 3; sample++)
        {
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var watch = Stopwatch.StartNew();
            compressed = Encode(fixture.Bytes, setting.Level, setting.Quality);
            watch.Stop();
            encodeMs += watch.Elapsed.TotalMilliseconds;
            allocations += GC.GetAllocatedBytesForCurrentThread() - allocated;
            watch.Restart();
            Decode(compressed, setting.Quality.HasValue);
            watch.Stop();
            decodeMs += watch.Elapsed.TotalMilliseconds;
        }
        Console.WriteLine(FormattableString.Invariant(
            $"{fixture.Name},{fixture.Bytes.Length},{setting.Name},{compressed.Length},{encodeMs / 3:F3},{decodeMs / 3:F3},{allocations / 3}"));
    }
}

static byte[] Encode(byte[] input, CompressionLevel? level, int? quality)
{
    using var output = new MemoryStream();
    using (Stream encoder = quality is { } value
        ? new BrotliStream(output, new BrotliCompressionOptions { Quality = value }, leaveOpen: true)
        : new GZipStream(output, level!.Value, leaveOpen: true))
        encoder.Write(input);
    return output.ToArray();
}

static byte[] Decode(byte[] input, bool brotli)
{
    using var encoded = new MemoryStream(input);
    using Stream decoder = brotli
        ? new BrotliStream(encoded, CompressionMode.Decompress)
        : new GZipStream(encoded, CompressionMode.Decompress);
    using var output = new MemoryStream();
    decoder.CopyTo(output);
    return output.ToArray();
}
