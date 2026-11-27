using System.Reflection;
using Steverzag.PropertySelectors.Extensions;

namespace Steverzag.PropertySelectors.MetaData
{
	/// <summary>
	/// Lists what a selector path can reach from <typeparamref name="T"/>, as a tree a UI can offer as a field picker.
	/// </summary>
	/// <remarks>
	/// The tree follows the parser's rules: under a collection it lists the element's properties, under a nullable value
	/// the underlying type's, and it leaves out indexers and fields. A type already on the way down from the root ends as a
	/// leaf. For the functions that can follow a node, see <see cref="FunctionCatalog.After"/>.
	/// </remarks>
	/// <typeparam name="T">The root type.</typeparam>
	public sealed class PropertiesExplorer<T> where T : class
	{
		private readonly Type _rootType = typeof(T);
		private readonly HashSet<Type> _visitedTypes = [typeof(T)];

		/// <summary>Builds the tree of what a selector path can reach from <typeparamref name="T"/>.</summary>
		/// <param name="maxDepth">How many levels of properties to list below the root.</param>
		/// <param name="filter">
		/// Leaves out the members it hides, and everything behind them. <see cref="MemberFilter.All"/> by default.
		/// </param>
		/// <returns>The root node, whose <see cref="SelectorsMetaDataTree.Path"/> is empty. Each call returns a new tree.</returns>
		public SelectorsMetaDataTree ExplorePropertiesTree(int maxDepth = 20, MemberFilter? filter = null)
		{
			var root = new SelectorsMetaDataTree(_rootType, "Root");
			ExploreProperties(_rootType, root, maxDepth, filter ?? MemberFilter.All, null);
			return root;
		}

		private void ExploreProperties(Type t, SelectorsMetaDataTree result, int depth, MemberFilter filter, (PropertyInfo Property, Type Type)? source)
		{
			if (depth == 0 || IsSimpleType(t))
				return;

			foreach (var property in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (property.GetIndexParameters().Length > 0 || !filter.Allows(new MemberContext(t, property, source?.Property, source?.Type)))
					continue;

				var path = result.Path.Length == 0 ? Segment(property.Name) : $"{result.Path}.{Segment(property.Name)}";
				var selectorType = _rootType.GetPropertyType((PropertySelector)path);
				var child = new SelectorsMetaDataTree(selectorType, property.Name)
				{
					Path = path,
					ElementType = SelectorExpressions.TryGetEnumerableElementType(selectorType, out var element) ? element : null,
				};
				result.Selectors.Add(child);

				var membersType = MembersType(property.PropertyType);
				if (_visitedTypes.Contains(membersType))
					continue;

				_visitedTypes.Add(membersType);
				ExploreProperties(membersType, child, depth - 1, filter, (property, t));
				_visitedTypes.Remove(membersType);
			}
		}

		private static Type MembersType(Type type)
		{
			if (SelectorExpressions.TryGetEnumerableElementType(type, out var element) && element is not null)
				type = element;

			return Nullable.GetUnderlyingType(type) ?? type;
		}

		private static bool IsSimpleType(Type t) =>
			t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(Guid);

		private static string Segment(string name) =>
			bool.TryParse(name, out _) ? $"{SelectorDefaults.PropertyNameEscape}{name}" : name;
	}
}
