using Sashiko.Maintenance.Commands;

namespace Sashiko.Maintenance.Tests
{
	// Prevent interference from other tests touching Console.Out or the dispatcher dictionary
	[Collection("NonParallel")]
	public sealed class CommandDispatcherTests
	{
		private static readonly string[] InvalidCategoryArguments = ["invalid"];
		private static readonly string[] TestRunArguments = ["test", "run"];
		private static readonly string[] TestCategoryArguments = ["test"];

		private sealed class FakeHandler : ICommandHandler
		{
			public bool Called { get; private set; }
			public string? ReceivedCommand { get; private set; }

			public Task DispatchAsync(string? command)
			{
				Called = true;
				ReceivedCommand = command;
				return Task.CompletedTask;
			}
		}

		[Fact]
		public async Task DispatchAsync_NoArgs_ShouldPrintHelp()
		{
			var originalOut = Console.Out;
			using var sw = new StringWriter();
			Console.SetOut(sw);

			try
			{
				await CommandDispatcher.DispatchAsync(Array.Empty<string>());

				var output = sw.ToString();
				Assert.Contains("Sashiko Maintenance Tool", output);
				Assert.Contains("Usage:", output);
				Assert.Contains("geography polish", output);
				Assert.Contains("languages update", output);
				Assert.Contains("names polish", output);
			}
			finally
			{
				Console.SetOut(originalOut);
			}
		}

		[Fact]
		public async Task DispatchAsync_UnknownCategory_ShouldPrintError()
		{
			var originalOut = Console.Out;
			using var sw = new StringWriter();
			Console.SetOut(sw);

			try
			{
				await CommandDispatcher.DispatchAsync(InvalidCategoryArguments);

				var output = sw.ToString();
				Assert.Contains("Unknown category", output);
				Assert.Contains("Usage:", output);
			}
			finally
			{
				Console.SetOut(originalOut);
			}
		}

		[Fact]
		public async Task DispatchAsync_KnownCategory_ShouldInvokeHandler()
		{
			var fake = new FakeHandler();

			// Inject handler
			CommandDispatcher.Handlers["test"] = fake;

			var originalOut = Console.Out;
			using var sw = new StringWriter();
			Console.SetOut(sw);

			try
			{
				await CommandDispatcher.DispatchAsync(TestRunArguments);

				Assert.True(fake.Called);
				Assert.Equal("run", fake.ReceivedCommand);
			}
			finally
			{
				// Clean up global state
				CommandDispatcher.Handlers.Remove("test");
				Console.SetOut(originalOut);
			}
		}

		[Fact]
		public async Task DispatchAsync_KnownCategoryWithoutCommand_ShouldPassNull()
		{
			var fake = new FakeHandler();

			CommandDispatcher.Handlers["test"] = fake;

			var originalOut = Console.Out;
			using var sw = new StringWriter();
			Console.SetOut(sw);

			try
			{
				await CommandDispatcher.DispatchAsync(TestCategoryArguments);

				Assert.True(fake.Called);
				Assert.Null(fake.ReceivedCommand);
			}
			finally
			{
				CommandDispatcher.Handlers.Remove("test");
				Console.SetOut(originalOut);
			}
		}
	}
}
