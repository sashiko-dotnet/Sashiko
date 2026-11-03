using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model
{
	public sealed record WorldRegion
	{
		[Required]
		public string Name { get; }

		[Required]
		public string M49Code { get; }

		[JsonConstructor]
		internal WorldRegion(string name, string m49Code)
		{
			Name = name;
			M49Code = m49Code;
		}
	}
}
