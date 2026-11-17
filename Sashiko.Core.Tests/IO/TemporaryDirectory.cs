namespace Sashiko.Core.Tests.IO
{
	internal sealed class TemporaryDirectory : IDisposable
	{
		public string Folder { get; } = Path.Combine(Path.GetTempPath(), $"sashiko-io-tests-{Guid.NewGuid():N}");

		public TemporaryDirectory() => Directory.CreateDirectory(Folder);

		public string FilePath(string name = "payload.bin") => Path.Combine(Folder, name);

		public void Dispose() => Directory.Delete(Folder, recursive: true);
	}

	internal sealed class UnixFactAttribute : FactAttribute
	{
		public UnixFactAttribute()
		{
			if (OperatingSystem.IsWindows())
				Skip = "Exercises Unix permissions or symbolic links without Windows developer-mode privileges.";
		}
	}
}
