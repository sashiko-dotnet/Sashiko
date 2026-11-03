using Sashiko.Geography.Model;

namespace Sashiko.Geography.Registry
{
	internal sealed class GeographyPopulationAnalysis
	{
		private readonly GeographyRegistry _registry;
		private readonly Dictionary<string, Subdivision> _subdivisions;
		private readonly Dictionary<string, Subdivision[]> _children;
		private readonly List<Subdivision> _postorder;
		private readonly Dictionary<string, decimal> _subdivisionCoverage = new(StringComparer.Ordinal);
		private readonly Dictionary<string, decimal> _cityCoverage = new(StringComparer.Ordinal);

		internal GeographyPopulationAnalysis(GeographyRegistry registry)
		{
			_registry = registry;
			_subdivisions = registry.Subdivisions.ToDictionary(item => item.Id);
			_children = registry.Subdivisions.GroupBy(item => item.ParentId ?? item.CountryCode)
				.ToDictionary(group => group.Key, group => group.ToArray());
			var traversal = new List<Subdivision>();
			var pending = new Stack<Subdivision>(registry.Subdivisions.Where(item => item.ParentId is null));
			while (pending.TryPop(out var current))
			{
				traversal.Add(current);
				foreach (var child in Children(current.Id)) pending.Push(child);
			}
			traversal.Reverse();
			_postorder = traversal;
		}

		internal void ValidateTotals()
		{
			var keys = _registry.Countries.Select(country => country.Population)
				.Concat(_registry.Subdivisions.Select(item => item.Population))
				.OfType<PopulationSnapshot>().Select(Key.For).Distinct();
			foreach (var key in keys)
			{
				var frontier = new Dictionary<string, decimal>(StringComparer.Ordinal);
				// A known parent replaces its descendants in the sum; unknown parents pass their known descendants upward.
				foreach (var subdivision in _postorder)
				{
					var total = Children(subdivision.Id).Sum(child => frontier[child.Id]);
					if (Matches(subdivision.Population, key))
					{
						NotGreater(total, subdivision.Population!, subdivision.Id, "subdivision");
						frontier[subdivision.Id] = subdivision.Population!.Value;
					}
					else frontier[subdivision.Id] = total;
				}
				foreach (var country in _registry.Countries.Where(country => Matches(country.Population, key)))
				{
					var total = Children(country.IsoAlpha2).Sum(child => frontier[child.Id]);
					NotGreater(total, country.Population!, country.IsoAlpha2, "subdivision");
					_subdivisionCoverage[country.IsoAlpha2] = total;
				}
			}

			var totals = new Dictionary<(string Id, Key Key), decimal>();
			foreach (var city in _registry.Cities.Where(city => city.Population is not null))
			{
				var key = Key.For(city.Population!);
				string? parent = city.SubdivisionId;
				while (parent is not null)
				{
					Add(totals, parent, key, city.Population!.Value);
					parent = _subdivisions[parent].ParentId;
				}
				Add(totals, city.CountryCode, key, city.Population!.Value);
			}
			foreach (var subdivision in _registry.Subdivisions.Where(item => item.Population is not null))
				NotGreater(totals.GetValueOrDefault((subdivision.Id, Key.For(subdivision.Population!))),
					subdivision.Population!, subdivision.Id, "municipality");
			foreach (var country in _registry.Countries.Where(country => country.Population is not null))
			{
				var total = totals.GetValueOrDefault((country.IsoAlpha2, Key.For(country.Population!)));
				NotGreater(total, country.Population!, country.IsoAlpha2, "municipality");
				_cityCoverage[country.IsoAlpha2] = total;
			}
		}

		internal CountryCoverage Coverage(Country country)
		{
			var denominator = country.Population?.Value;
			return new CountryCoverage(country.IsoAlpha2,
				_registry.Subdivisions.Count(item => item.CountryCode == country.IsoAlpha2),
				_registry.Cities.Count(item => item.CountryCode == country.IsoAlpha2),
				denominator is > 0 ? _subdivisionCoverage.GetValueOrDefault(country.IsoAlpha2) / denominator.Value : null,
				denominator is > 0 ? _cityCoverage.GetValueOrDefault(country.IsoAlpha2) / denominator.Value : null);
		}

		private Subdivision[] Children(string id) => _children.GetValueOrDefault(id) ?? [];
		private static bool Matches(PopulationSnapshot? value, Key key) => value is not null && Key.For(value) == key;
		private static void Add(Dictionary<(string Id, Key Key), decimal> totals, string id, Key key, long value)
			=> totals[(id, key)] = totals.GetValueOrDefault((id, key)) + value;
		private void NotGreater(decimal total, PopulationSnapshot parent, string id, string scope)
			=> GeographyDataValidator.Require(total <= parent.Value, _registry.Paths[id], "Population.Value",
				$"Comparable {scope} total {total} exceeds parent population {parent.Value}.");
		private readonly record struct Key(DateOnly Date, string Definition, string BoundaryVersion)
		{
			internal static Key For(PopulationSnapshot value) => new(value.ReferenceDate, value.Definition, value.BoundaryVersion);
		}
	}
}
