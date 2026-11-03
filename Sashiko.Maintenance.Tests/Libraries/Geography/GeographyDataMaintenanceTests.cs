using Sashiko.Geography.Api;
using Sashiko.Maintenance.Libraries.Geography;

namespace Sashiko.Maintenance.Tests.Libraries.Geography
{
	public sealed class GeographyDataMaintenanceTests
	{
		private sealed class FakePolisher : IGeographyDataPolisher
		{
			public string? ReceivedPath { get; private set; }

			public Task<GeographyPolishResult> PolishAsync(string dataDirectory)
			{
				ReceivedPath = dataDirectory;
				return Task.FromResult(new GeographyPolishResult(1, 2, 3));
			}
		}

		[Fact]
		public async Task PolishEmbeddedGeographyAsync_ShouldUseGeographyDataDirectory()
		{
			var polisher = new FakePolisher();

			string Locate(string projectName, Type type)
			{
				Assert.Equal("Sashiko.Geography", projectName);
				Assert.Equal(typeof(GeographyService), type);
				return "/fake/geography";
			}

			await GeographyDataMaintenance.PolishEmbeddedGeographyAsync(polisher, Locate);

			Assert.Equal(
				"/fake/geography/Data",
				polisher.ReceivedPath?.Replace("\\", "/"));
		}
	}
}
