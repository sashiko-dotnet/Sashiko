using Sashiko.Maintenance.Libraries.Geography;

namespace Sashiko.Maintenance.Commands
{
	internal sealed class GeographyCommandHandler : ICommandHandler
	{
		internal Func<string, Task> Run { get; set; } = GeographyDataMaintenance.RunAsync;

		public Task DispatchAsync(string? command)
		{
			if (command is not ("polish" or "validate" or "check" or "validate-release"))
				throw new ArgumentException("Available geography commands: polish, validate, check, validate-release.", nameof(command));
			return Run(command);
		}
	}
}
