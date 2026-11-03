namespace Sashiko.Geography.Model
{
	/// <summary>Population coverage uses only observations comparable with the country's snapshot.</summary>
	public sealed record CountryCoverage(
		string CountryCode,
		int SubdivisionCount,
		int CityCount,
		decimal? SubdivisionPopulationCoverage,
		decimal? CityPopulationCoverage);
}
