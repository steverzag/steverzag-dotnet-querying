using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	/// <summary>
	/// How FilterNodes combine (decision 007 in docs/decisions.md):
	/// <list type="bullet">
	/// <item>Nodes join strictly left to right. Each node's connector says how it joins everything before it,
	/// and the first node's connector is ignored.</item>
	/// <item>There is no AND-before-OR precedence: <c>A or B and C</c> means <c>(A or B) and C</c>.</item>
	/// <item>A <see cref="GroupNode"/> is parentheses around one or more child nodes. It has no condition of its own
	/// (decision 009.5 holds by construction).</item>
	/// </list>
	/// </summary>
	public class FilterNodeLogicTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		// 1 Hammer | Tools  | Qty 100 | active   | Active       | [tools, steel]
		// 2 Shovel | Garden | Qty 0   | active   | Draft        | [garden]
		// 3 Wrench | Tools  | Qty 50  | inactive | Discontinued | []
		// 4 Rake   | Garden | Qty 200 | active   | Active       | [garden, tools]

		private static FilterNode NameIs(string name) => Node(nameof(Product.Name), FilterCondition.Equals, name);
		private static FilterNode IsActive(bool value) => Node(nameof(Product.IsActive), FilterCondition.Equals, value);
		private static FilterNode QuantityGreaterThan(int value) => Node(nameof(Product.Quantity), FilterCondition.GreaterThan, value);
		private static FilterNode CategoryIs(string name) => Node("Category.Name", FilterCondition.Equals, name);
		private static FilterNode StatusIs(ProductStatus status) => Node(nameof(Product.Status), FilterCondition.Equals, (int)status);

		private static FilterNode Or(FilterNode node) => node with { FilterLogicOption = LogicalConnector.Or };
		private static FilterNode And(FilterNode node) => node with { FilterLogicOption = LogicalConnector.And };
		private static GroupNode Group(params FilterNode[] children) => new() { Filters = children };

		// ---- basics

		[Fact]
		public void And_RequiresBoth()
		{
			Assert.Equal([4], FilterIds([CategoryIs("Garden"), And(QuantityGreaterThan(50))]));
		}

		[Fact]
		public void Or_RequiresEither()
		{
			Assert.Equal([1, 4], FilterIds([NameIs("Hammer"), Or(NameIs("Rake"))]));
		}

		[Fact]
		public void SameFieldTwice_FormsARange()
		{
			Assert.Equal([1, 3], FilterIds(
			[
				Node(nameof(Product.Quantity), FilterCondition.GreaterThanOrEqual, 50),
				And(Node(nameof(Product.Quantity), FilterCondition.LessThanOrEqual, 100)),
			]));
		}

		[Fact]
		public void FirstNodeConnector_IsIgnored()
		{
			// Marking the first node Or has nothing to join with, so it reads the same as And.
			int[] withOrFirst = FilterIds([Or(QuantityGreaterThan(50)), And(IsActive(true))]);
			int[] withAndFirst = FilterIds([And(QuantityGreaterThan(50)), And(IsActive(true))]);

			Assert.Equal([1, 4], withOrFirst);
			Assert.Equal(withAndFirst, withOrFirst);
		}

		[Fact]
		public void Contradiction_ReturnsNothing()
		{
			Assert.Empty(FilterIds([NameIs("Hammer"), And(NameIs("Rake"))]));
		}

		[Fact]
		public void EmptyList_ReturnsEveryRow()
		{
			Assert.Equal([1, 2, 3, 4], FilterIds([]));
		}

		[Fact]
		public void NullList_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => FilterIds(null!));
		}

		// ---- evaluation order: strictly left to right, no precedence

		[Fact]
		public void OrThenAnd_IsReadLeftToRight_NotWithAndPrecedence()
		{
			// Hammer OR Shovel AND inactive
			//   left to right (ours):  (Hammer or Shovel) and inactive → nothing, both are active
			//   SQL/C# precedence:     Hammer or (Shovel and inactive) → Hammer
			Assert.Empty(FilterIds([NameIs("Hammer"), Or(NameIs("Shovel")), And(IsActive(false))]));
		}

		[Fact]
		public void OrThenAnd_AppliesTheAndToEverythingBefore()
		{
			// (Hammer or Wrench) and active → Hammer, because Wrench is inactive
			Assert.Equal([1], FilterIds([NameIs("Hammer"), Or(NameIs("Wrench")), And(IsActive(true))]));
		}

		[Fact]
		public void Group_GivesTheUsualPrecedence()
		{
			// Hammer or (Shovel and inactive) → Hammer
			Assert.Equal([1], FilterIds([NameIs("Hammer"), Or(Group(NameIs("Shovel"), And(IsActive(false))))]));
		}

		// ---- groups

		[Fact]
		public void GroupFirst_ThenPlainNode()
		{
			// (Hammer or Rake) and qty > 150
			Assert.Equal([4], FilterIds([Group(NameIs("Hammer"), Or(NameIs("Rake"))), And(QuantityGreaterThan(150))]));
		}

		[Fact]
		public void GroupBetweenPlainNodes()
		{
			// ((active and (Tools or Discontinued)) or Shovel)
			//   active and (Tools or Discontinued) → Hammer (Wrench is Tools and Discontinued, but inactive)
			Assert.Equal([1, 2], FilterIds(
			[
				IsActive(true),
				And(Group(CategoryIs("Tools"), Or(StatusIs(ProductStatus.Discontinued)))),
				Or(NameIs("Shovel")),
			]));
		}

		[Fact]
		public void GroupConnector_JoinsTheWholeGroup()
		{
			FilterNode GardenOver100() => Group(CategoryIs("Garden"), And(QuantityGreaterThan(100)));

			// Hammer or (Garden and qty > 100) → Hammer, Rake
			Assert.Equal([1, 4], FilterIds([NameIs("Hammer"), Or(GardenOver100())]));
			// Hammer and (Garden and qty > 100) → nothing
			Assert.Empty(FilterIds([NameIs("Hammer"), And(GardenOver100())]));
		}

		[Fact]
		public void FirstChildConnector_InsideAGroup_IsIgnored()
		{
			Assert.Equal([1], FilterIds([Group(Or(NameIs("Hammer")), And(IsActive(true)))]));
		}

		[Fact]
		public void OrGroupInsideAnd_WithCollectionAndNavigation()
		{
			// qty > 0 and (tagged steel or in Garden)
			//   qty > 0 → 1, 3, 4. Tagged steel → 1. Garden → 2, 4.
			Assert.Equal([1, 4], FilterIds(
			[
				QuantityGreaterThan(0),
				And(Group(
					Node(nameof(Product.Tags), FilterCondition.ContainsAny, new List<object> { "steel" }),
					Or(CategoryIs("Garden")))),
			]));
		}

		[Fact]
		public void SingleChildGroups_NestedThreeDeep_ActLikeTheChild()
		{
			Assert.Equal([4], FilterIds([Group(Group(Group(NameIs("Rake"))))]));
		}

		[Fact]
		public void GroupsNestedThreeDeep()
		{
			// Wrench or (active and (qty < 10 or (Hammer and Active)))
			//   innermost: Hammer and Active → 1
			//   middle:    qty < 10 → 2, or the innermost → 1, 2
			//   outer:     active → 1, 2, 4, so and the middle → 1, 2
			//   top:       Wrench → 3, so or the outer → 1, 2, 3
			Assert.Equal([1, 2, 3], FilterIds(
			[
				NameIs("Wrench"),
				Or(Group(
					IsActive(true),
					And(Group(
						Node(nameof(Product.Quantity), FilterCondition.LessThan, 10),
						Or(Group(
							NameIs("Hammer"),
							And(StatusIs(ProductStatus.Active)))))))),
			]));
		}

		// ---- node shapes
		// A node is either a ConditionNode or a GroupNode, so a group can't carry a condition of its own
		// (decision 009.5 holds by construction). The remaining invariants are checked when a node is built.

		[Fact]
		public void GroupWithConnector_JoinsLikeAnyNode()
		{
			Assert.Equal([1, 4], FilterIds([NameIs("Hammer"), Or(Group(NameIs("Rake")))]));
		}

		[Fact]
		public void GroupWithNoChildren_Throws()
		{
			// Used to act as a plain node, applying the node's own condition.
			Assert.Throws<ArgumentException>(() => new GroupNode { Filters = [] });
		}

		[Fact]
		public void GroupWithNullChildList_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => new GroupNode { Filters = null! });
		}

		[Fact]
		public void GroupWithANullChild_Throws()
		{
			Assert.Throws<ArgumentException>(() => new GroupNode { Filters = [NameIs("Rake"), null!] });
		}

		[Fact]
		public void Group_EmptiedThroughWith_Throws()
		{
			// `with` goes through the same init accessor, so a copy can't bypass the check.
			Assert.Throws<ArgumentException>(() => Group(NameIs("Rake")) with { Filters = [] });
		}

		[Fact]
		public void Group_CopiesItsChildren()
		{
			var children = new List<FilterNode> { NameIs("Rake") };
			var group = new GroupNode { Filters = children };

			children.Add(Or(NameIs("Hammer")));

			Assert.Equal([4], FilterIds([group]));
		}

		[Fact]
		public void ConditionWithNullField_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => new ConditionNode { FieldSelector = null!, FilterCondition = FilterCondition.IsEmpty });
		}

		[Fact]
		public void NullNodeInTheList_Throws()
		{
			Assert.Throws<ArgumentException>(() => FilterIds([NameIs("Rake"), null!]));
		}

		// ---- mixed types in one tree, translated to a single SQL query

		[Fact]
		public void MixedTypeTree_Translates()
		{
			// ((OptionalStatus is Draft or Description starts with "Steel") and NullableQuantity > 150)
			//   or (no tags and DeletedAt has a value)
			//   first group → 4 (Draft), 1 (Steel head). Then and NullableQty > 150 → 4.
			//   second group → 3 (no tags, deleted).
			Assert.Equal([3, 4], FilterIds(
			[
				Group(
					Node(nameof(Product.OptionalStatus), FilterCondition.Equals, (int)ProductStatus.Draft),
					Or(Node(nameof(Product.Description), FilterCondition.StartsWith, "Steel"))),
				And(Node(nameof(Product.NullableQuantity), FilterCondition.GreaterThan, 150)),
				Or(Group(
					Node(nameof(Product.Tags), FilterCondition.IsEmpty),
					And(Node(nameof(Product.DeletedAt), FilterCondition.IsNotEmpty)))),
			]));
		}

		[Fact]
		public void ApplyLogicsFilter_DoesNotWriteToConsole()
		{
			// Decision 008 cleanup: it used to print every filter expression. A library must not write to stdout.
			var original = Console.Out;
			using var captured = new StringWriter();
			Console.SetOut(captured);
			try
			{
				FilterIds([NameIs("Hammer"), Or(StatusIs(ProductStatus.Draft))]);
			}
			finally
			{
				Console.SetOut(original);
			}

			Assert.Equal(string.Empty, captured.ToString());
		}
	}
}
