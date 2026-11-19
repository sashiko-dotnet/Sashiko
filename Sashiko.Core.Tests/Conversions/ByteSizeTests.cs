using System.Globalization;
using Sashiko.Core.Conversions;
using Sashiko.Core.Models.Enums;

namespace Sashiko.Core.Tests.Conversions
{
	public sealed class ByteSizeTests
	{
		[Theory]
		[InlineData(0, "0 B")]
		[InlineData(1, "1 B")]
		[InlineData(1023, "1,023 B")]
		[InlineData(1024, "1 KiB")]
		[InlineData(1_572_864, "1.5 MiB")]
		[InlineData(-1_572_864, "-1.5 MiB")]
		[InlineData(long.MinValue, "-8 EiB")]
		[InlineData(long.MaxValue, "8 EiB")]
		public void Format_DefaultsToBinaryUnits(long bytes, string expected)
		{
			Assert.Equal(expected, ByteSize.Format(bytes, formatProvider: CultureInfo.InvariantCulture));
		}

		[Theory]
		[InlineData(999, "999 B")]
		[InlineData(1_000, "1 kB")]
		[InlineData(1_125, "1.13 kB")]
		[InlineData(-1_125, "-1.13 kB")]
		[InlineData(1_500_000, "1.5 MB")]
		[InlineData(long.MinValue, "-9.22 EB")]
		public void Format_SupportsDecimalUnits(long bytes, string expected)
		{
			Assert.Equal(expected, ByteSize.Format(bytes, ByteUnitStyle.Decimal, formatProvider: CultureInfo.InvariantCulture));
		}

		[Theory]
		[InlineData(1_048_575, ByteUnitStyle.Binary, 2, "1 MiB")]
		[InlineData(1_048_575, ByteUnitStyle.Binary, 3, "1,023.999 KiB")]
		[InlineData(999_994, ByteUnitStyle.Decimal, 2, "999.99 kB")]
		[InlineData(999_995, ByteUnitStyle.Decimal, 2, "1 MB")]
		[InlineData(-999_995, ByteUnitStyle.Decimal, 2, "-1 MB")]
		[InlineData(999_500, ByteUnitStyle.Decimal, 0, "1 MB")]
		public void Format_PromotesUnitsWhenRoundingReachesTheNextUnit(
			long bytes, ByteUnitStyle style, int decimalPlaces, string expected)
		{
			Assert.Equal(expected, ByteSize.Format(bytes, style, decimalPlaces, CultureInfo.InvariantCulture));
		}

		[Fact]
		public void Format_PreservesIntegerPrecisionBeforeRoundingLargeSizes()
		{
			Assert.Equal("9.223372036854775807 EB",
				ByteSize.Format(long.MaxValue, ByteUnitStyle.Decimal, 18, CultureInfo.InvariantCulture));
		}

		[Fact]
		public void Format_UsesTheSuppliedNumberFormat()
		{
			var culture = CultureInfo.GetCultureInfo("it-IT");

			Assert.Equal("1,5 MiB", ByteSize.Format(1_572_864, formatProvider: culture));
			Assert.Equal("1.023 B", ByteSize.Format(1023, formatProvider: culture));
		}

		[Fact]
		public void Format_RejectsInvalidOptions()
		{
			Assert.Throws<ArgumentOutOfRangeException>("style", () => ByteSize.Format(0, (ByteUnitStyle)(-1)));
			Assert.Throws<ArgumentOutOfRangeException>("decimalPlaces", () => ByteSize.Format(0, decimalPlaces: -1));
			Assert.Throws<ArgumentOutOfRangeException>("decimalPlaces", () => ByteSize.Format(0, decimalPlaces: 29));
		}
	}
}
