using Sashiko.Maintenance.Libraries.Geography;

namespace Sashiko.Maintenance.Commands
{
	internal class GeographyCommandHandler : ICommandHandler
	{
		internal Func<Task>? PolishHook { get; set; }
			= () => GeographyDataMaintenance.PolishEmbeddedGeographyAsync();
		internal Func<Task>? ValidateHook { get; set; }
			= () => GeographyDataMaintenance.ValidateEmbeddedGeographyAsync();
		internal Func<Task>? ReleaseHook { get; set; }
			= () => GeographyDataMaintenance.ValidateReleaseAsync();

		public async Task DispatchAsync(string? command)
		{
			switch (command)
			{
				case "validate-release":
					await (ReleaseHook?.Invoke() ?? Task.CompletedTask);
					break;
				case "validate":
					await (ValidateHook?.Invoke() ?? Task.CompletedTask);
					break;
				case "polish":
					await (PolishHook?.Invoke() ?? Task.CompletedTask);
					break;

				default:
					Console.WriteLine("Unknown geography command.");
					Console.WriteLine("Available: polish, validate, validate-release");
					break;
			}
		}
	}
}
