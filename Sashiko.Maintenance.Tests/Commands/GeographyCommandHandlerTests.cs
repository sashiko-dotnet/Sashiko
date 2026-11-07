using Sashiko.Maintenance.Commands;

namespace Sashiko.Maintenance.Tests.Commands
{
	public sealed class GeographyCommandHandlerTests
	{
		[Theory]
		[InlineData("polish")]
		[InlineData("validate")]
		[InlineData("check")]
		[InlineData("validate-release")]
		public async Task RecognizedCommands_ShouldDispatchOnce(string command)
		{
			var received = new List<string>();
			var handler = new GeographyCommandHandler { Run = value => { received.Add(value); return Task.CompletedTask; } };
			await handler.DispatchAsync(command);
			Assert.Equal(command, Assert.Single(received));
		}

		[Theory]
		[InlineData(null)]
		[InlineData("unknown")]
		public async Task UnknownCommands_ShouldFailInsteadOfReportingSuccess(string? command)
		{
			await Assert.ThrowsAsync<ArgumentException>(() => new GeographyCommandHandler().DispatchAsync(command));
		}
	}
}
