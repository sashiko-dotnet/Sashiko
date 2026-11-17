using Sashiko.Validation.Schema.Inspectors.CSharp;

namespace Sashiko.Validation.Tests.Schema.Inspectors.CSharp
{
	public sealed class RepeatedTypeTests
	{
		private sealed class Detail
		{
			public int Value { get; set; }
		}

		private sealed class Siblings
		{
			public Detail? First { get; set; }
			public Detail? Second { get; set; }
		}

		private sealed class RepeatedCollection
		{
			public Detail? First { get; set; }
			public List<Detail>? Items { get; set; }
		}

		private sealed class RecursiveSiblings
		{
			public Node? First { get; set; }
			public Node? Second { get; set; }
		}

		private sealed class Node
		{
			public int Value { get; set; }
			public Node? Child { get; set; }
		}

		[Fact]
		public void GetSchema_RepeatedSiblingTypes_AreFullyInspected()
		{
			var fields = CSharpSchemaInspector.GetSchema(typeof(Siblings)).Fields!;

			Assert.True(fields[nameof(Siblings.First)].Fields!.ContainsKey(nameof(Detail.Value)));
			Assert.True(fields[nameof(Siblings.Second)].Fields!.ContainsKey(nameof(Detail.Value)));
		}

		[Fact]
		public void GetSchema_TypeReusedAsCollectionElement_IsFullyInspected()
		{
			var fields = CSharpSchemaInspector.GetSchema(typeof(RepeatedCollection)).Fields!;

			Assert.True(fields[nameof(RepeatedCollection.First)].Fields!.ContainsKey(nameof(Detail.Value)));
			Assert.True(fields[nameof(RepeatedCollection.Items)].Element!.Fields!.ContainsKey(nameof(Detail.Value)));
		}

		[Fact]
		public void GetSchema_RepeatedRecursiveTypes_PreserveAncestorCycleProtection()
		{
			var fields = CSharpSchemaInspector.GetSchema(typeof(RecursiveSiblings)).Fields!;

			foreach (var name in new[] { nameof(RecursiveSiblings.First), nameof(RecursiveSiblings.Second) })
			{
				var node = fields[name];
				Assert.True(node.Fields!.ContainsKey(nameof(Node.Value)));
				Assert.Empty(node.Fields[nameof(Node.Child)].Fields!);
			}
		}
	}
}
