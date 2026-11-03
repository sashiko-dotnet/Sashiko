namespace Sashiko.Maintenance.Libraries.Geography
{
	internal interface IGeographyDataPolisher
	{
		Task<GeographyPolishResult> PolishAsync(string dataDirectory);
	}
}
