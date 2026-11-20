using Sashiko.Core.Models.Enums;

namespace Sashiko.Core.Conversions
{
	/// <summary>Formats exact byte counts for display in binary or decimal units.</summary>
	public static class ByteSize
	{
		private static readonly string[] BinarySymbols = ["B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB"];
		private static readonly string[] DecimalSymbols = ["B", "kB", "MB", "GB", "TB", "PB", "EB"];

		/// <summary>Formats a byte count with an automatically selected unit and localized number.</summary>
		/// <param name="bytes">The byte count. Negative values can represent decreases.</param>
		/// <param name="style">The unit system. Defaults to binary IEC units.</param>
		/// <param name="decimalPlaces">The maximum fractional digits, from zero through 28. Trailing zeros are omitted.</param>
		/// <param name="formatProvider">The number format provider. Defaults to the current culture.</param>
		/// <returns>A localized number, a space, and the unit symbol.</returns>
		/// <exception cref="ArgumentOutOfRangeException">The unit style or number of decimal places is invalid.</exception>
		public static string Format(long bytes, ByteUnitStyle style = ByteUnitStyle.Binary,
			int decimalPlaces = 2, IFormatProvider? formatProvider = null)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(decimalPlaces);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(decimalPlaces, 28);
			var (radix, symbols) = style switch
			{
				ByteUnitStyle.Binary => (1024, BinarySymbols),
				ByteUnitStyle.Decimal => (1000, DecimalSymbols),
				_ => throw new ArgumentOutOfRangeException(nameof(style))
			};

			// Convert before taking the absolute value to support long.MinValue.
			var magnitude = Math.Abs((decimal)bytes);
			var unit = 0;
			long divisor = 1;
			while (unit < symbols.Length - 1 && magnitude >= divisor * radix)
			{
				divisor *= radix;
				unit++;
			}

			var rounded = decimal.Round(magnitude / divisor, decimalPlaces, MidpointRounding.AwayFromZero);
			if (unit < symbols.Length - 1 && rounded >= radix)
			{
				// A value below the next unit that rounds up to it displays as one of that unit.
				rounded = 1;
				unit++;
			}

			var value = bytes < 0 ? -rounded : rounded;
			var numberFormat = decimalPlaces == 0 ? "#,0" : "#,0." + new string('#', decimalPlaces);
			return string.Concat(value.ToString(numberFormat, formatProvider), " ", symbols[unit]);
		}
	}
}
