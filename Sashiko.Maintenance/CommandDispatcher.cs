using Sashiko.Maintenance.Commands;

namespace Sashiko.Maintenance
{
	internal static class CommandDispatcher
	{
		internal static readonly Dictionary<string, ICommandHandler> Handlers =
			new(StringComparer.OrdinalIgnoreCase)
			{
				["geography"] = new GeographyCommandHandler(),
				["languages"] = new LanguageCommandHandler(),
				["names"] = new NamesCommandHandler()
			};

		public static async Task DispatchAsync(string[] args)
		{
			Console.WriteLine("Sashiko Maintenance Tool");

			if (args.Length == 0)
			{
				PrintHelp();
				return;
			}

			var category = args[0];
			var command = args.Length > 1 ? args[1] : null;

			if (Handlers.TryGetValue(category, out var handler))
			{
				await handler.DispatchAsync(command);
				return;
			}

			Console.WriteLine($"Unknown category: {category}");
			PrintHelp();
		}

		private static void PrintHelp()
		{
			Console.WriteLine("Usage:");
			Console.WriteLine("  geography polish");
			Console.WriteLine("  geography validate");
			Console.WriteLine("  geography check");
			Console.WriteLine("  geography validate-release");
			Console.WriteLine("  languages update");
			Console.WriteLine("  names polish");
		}
	}
}
