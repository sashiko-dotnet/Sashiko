using Sashiko.Maintenance.Commands;

namespace Sashiko.Maintenance.Tests.Commands
{
	[Collection("NonParallel")]
	public sealed class GeographyCommandHandlerTests
	{
		[Fact]
		public async Task DispatchAsync_Validate_ShouldInvokeOnlyValidation()
		{
			var called = false;
			var handler = new GeographyCommandHandler
			{
				ValidateHook = () => { called = true; return Task.CompletedTask; },
				PolishHook = () => throw new InvalidOperationException("Validation must not polish.")
			};
			await handler.DispatchAsync("validate");
			Assert.True(called);
		}

		[Fact]
		public async Task DispatchAsync_Polish_ShouldInvokeHook()
		{
			var called = false;
			var handler = new GeographyCommandHandler
			{
				PolishHook = () =>
				{
					called = true;
					return Task.CompletedTask;
				}
			};

			await handler.DispatchAsync("polish");

			Assert.True(called);
		}

		[Fact]
		public async Task DispatchAsync_Polish_ShouldNotThrow_WhenHookIsNull()
		{
			var handler = new GeographyCommandHandler
			{
				PolishHook = null
			};

			var exception = await Record.ExceptionAsync(() => handler.DispatchAsync("polish"));

			Assert.Null(exception);
		}

		[Fact]
		public async Task DispatchAsync_UnknownCommand_ShouldPrintMessage()
		{
			var originalOut = Console.Out;
			using var writer = new StringWriter();
			Console.SetOut(writer);

			try
			{
				await new GeographyCommandHandler().DispatchAsync("invalid");

				Assert.Contains("Unknown geography command", writer.ToString());
				Assert.Contains("Available: polish", writer.ToString());
			}
			finally
			{
				Console.SetOut(originalOut);
			}
		}
	}
}
