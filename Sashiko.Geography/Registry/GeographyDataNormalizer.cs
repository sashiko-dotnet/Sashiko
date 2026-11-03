using System.Globalization;
using Sashiko.Geography.Model;
using Sashiko.Geography.Model.Data;

namespace Sashiko.Geography.Registry
{
	internal sealed class GeographyDataNormalizer(string path, bool polish)
	{
		internal CountryData Normalize(CountryData data) => data with
		{
			Name = Text(data.Name, "Name"),
			IsoAlpha2 = Text(data.IsoAlpha2, "IsoAlpha2", upper: true),
			IsoAlpha3 = Text(data.IsoAlpha3, "IsoAlpha3", upper: true),
			IsoNumeric = Text(data.IsoNumeric, "IsoNumeric"),
			M49RegionCode = Text(data.M49RegionCode, "M49RegionCode"),
			CapitalCityIds = Values(data.CapitalCityIds, "CapitalCityIds", upper: true),
			OfficialLanguageCodes = Values(data.OfficialLanguageCodes, "OfficialLanguageCodes", lower: true),
			SourceIds = Values(data.SourceIds, "SourceIds", upper: true),
			Population = Normalize(data.Population)
		};

		internal SubdivisionData Normalize(SubdivisionData data) => data with
		{
			Id = Text(data.Id, "Id", upper: true),
			Name = Text(data.Name, "Name"),
			Type = Text(data.Type, "Type"),
			ParentId = Optional(data.ParentId, "ParentId", upper: true),
			Iso3166_2 = Optional(data.Iso3166_2, "Iso3166_2", upper: true),
			SourceIds = Values(data.SourceIds, "SourceIds", upper: true),
			Population = Normalize(data.Population)
		};

		internal CityData Normalize(CityData data) => data with
		{
			Id = Text(data.Id, "Id", upper: true),
			Name = Text(data.Name, "Name"),
			SubdivisionId = Optional(data.SubdivisionId, "SubdivisionId", upper: true),
			UnLocode = Optional(data.UnLocode, "UnLocode", upper: true),
			SourceIds = Values(data.SourceIds, "SourceIds", upper: true),
			Population = Normalize(data.Population)
		};

		private PopulationData? Normalize(PopulationData? data)
		{
			if (data is null) return null;
			var date = Text(data.ReferenceDate, "Population.ReferenceDate");
			if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
				|| parsed == default)
				throw Error("Population.ReferenceDate", "Expected a nonzero ISO date (yyyy-MM-dd).");
			return data with
			{
				ReferenceDate = date,
				Definition = Text(data.Definition, "Population.Definition", lower: true),
				BoundaryVersion = Text(data.BoundaryVersion, "Population.BoundaryVersion", upper: true),
				SourceId = Text(data.SourceId, "Population.SourceId", upper: true)
			};
		}

		private IReadOnlyList<string> Values(IReadOnlyList<string>? values, string property, bool upper = false, bool lower = false)
		{
			if (values is null) throw Error(property, "An array is required.");
			var normalized = values.Select(value => Text(value, property, upper, lower)).ToArray();
			if (normalized.Distinct(StringComparer.Ordinal).Count() != normalized.Length)
				throw Error(property, "Duplicate value.");
			var sorted = normalized.Order(StringComparer.Ordinal).ToArray();
			if (!polish && !normalized.SequenceEqual(sorted)) throw Error(property, "Values must be sorted.");
			return Array.AsReadOnly(sorted);
		}

		private string? Optional(string? value, string property, bool upper = false)
			=> value is null ? null : Text(value, property, upper);

		private string Text(string? value, string property, bool upper = false, bool lower = false)
		{
			if (string.IsNullOrWhiteSpace(value)) throw Error(property, "A nonblank value is required.");
			var normalized = value.Trim().Normalize();
			if (upper) normalized = normalized.ToUpperInvariant();
			if (lower) normalized = normalized.ToLowerInvariant();
			if (!polish && value != normalized) throw Error(property, "Value must be normalized; run geography polish.");
			return normalized;
		}

		private InvalidDataException Error(string property, string message) => new($"{path}: {property}: {message}");
	}
}
