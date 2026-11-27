using System.Reflection;
using Steverzag.PropertySelectors.Extensions;

namespace Steverzag.PropertySelectors.MetaData
{
	public class PropertiesExplorer<T> where T : class
	{
		private readonly Type _rootType = typeof(T);
		private SelectorsMetaDataTree Result { get; set; }
		private readonly HashSet<Type> _visitedTypes = [typeof(T)];

		public PropertiesExplorer()
		{
			Result = new SelectorsMetaDataTree(_rootType, "Root");
		}

		public SelectorsMetaDataTree ExplorePropertiesTree(int maxDepth = 20, MemberFilter? filter = null)
		{
			ExploreProperties(_rootType, Result, maxDepth, filter ?? MemberFilter.All, null);
			return Result;
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
					ElementType = ExpressionUtils.TryGetEnumerableElementType(selectorType, out var element) ? element : null,
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
			if (ExpressionUtils.TryGetEnumerableElementType(type, out var element) && element is not null)
				type = element;

			return Nullable.GetUnderlyingType(type) ?? type;
		}

		private static bool IsSimpleType(Type t) =>
			t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(Guid);

		private static string Segment(string name) =>
			bool.TryParse(name, out _) ? $"{SelectorDefaults.PropertyNameEscape}{name}" : name;
	}
}
