namespace Sashiko.Core.Models.Enums
{
	/// <summary>Chooses the unit system used to display a byte count.</summary>
	public enum ByteUnitStyle
	{
		/// <summary>IEC units based on powers of 1024, such as KiB and MiB.</summary>
		Binary,

		/// <summary>SI units based on powers of 1000, such as kB and MB.</summary>
		Decimal
	}
}
