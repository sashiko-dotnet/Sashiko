using System.Collections.ObjectModel;
using Sashiko.Geography.Model;

namespace Sashiko.Maintenance.Libraries.Geography
{
	internal sealed class GeographyDataSet
	{
		private readonly IReadOnlyDictionary<object, string> _locations;
		internal IReadOnlyDictionary<string, object> Documents { get; }
		internal IReadOnlyList<WorldRegion> WorldRegions { get; }
		internal IReadOnlyList<Country> Countries { get; }
		internal IReadOnlyList<Region> Regions { get; }
		internal IReadOnlyList<City> Cities { get; }
		internal IReadOnlyList<GeographySource> Sources { get; }

		internal GeographyDataSet(Dictionary<string, object> documents, Dictionary<object, string> locations)
		{
			_locations = new ReadOnlyDictionary<object, string>(locations);
			Documents = new ReadOnlyDictionary<string, object>(documents);
			WorldRegions = All<WorldRegion>();
			Countries = All<Country>();
			Regions = All<Region>();
			Cities = All<City>();
			Sources = All<GeographySource>();
		}

		internal string SourceOf(object value) => _locations[value];
		private IReadOnlyList<T> All<T>() => Array.AsReadOnly(Documents.Values
			.OfType<IReadOnlyList<T>>().SelectMany(values => values).ToArray());
	}
}
