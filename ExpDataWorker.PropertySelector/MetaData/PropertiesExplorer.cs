namespace ExpDataWorker.PropertySelectors.MetaData
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

		public SelectorsMetaDataTree ExplorePropertiesTree(int maxDepth = 20)
		{
			ExploreProperties(_rootType, Result, maxDepth, includeCircularReferences: false);
			return Result;
		}

		private void ExploreProperties(Type t, SelectorsMetaDataTree result, int depth, bool includeCircularReferences)
		{
			if (depth == 0) return;

			var isSimpleType = t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(Guid);

			if(isSimpleType)
				return;

			var properties = t.GetProperties();

			if (properties.Length == 0)
				return;

			for (int i = 0; i < properties.Length; i++)
			{
				var property = properties[i];

				if (!includeCircularReferences && _visitedTypes.Contains(property.PropertyType))
					continue;

				var child = new SelectorsMetaDataTree(property.PropertyType, property.Name);
				result.Selectors.Add(child);

				_visitedTypes.Add(property.PropertyType);
				ExploreProperties(property.PropertyType, child, depth - 1, includeCircularReferences);
				_visitedTypes.Remove(property.PropertyType);
			}
		}
	}
}