using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors
{
	internal class GenericPropertySelectorFactory
	{
		public static PropertySelector CreateGeneric(Type propertySelectorType, Type innerType)
		{
			if (propertySelectorType.IsAssignableTo(typeof(PropertySelector)) == false)
			{
				throw new ArgumentException($"The provided type {propertySelectorType.FullName} is not a PropertySelector.");
			}

			var genericType = propertySelectorType.MakeGenericType(innerType);
			return (PropertySelector)Activator.CreateInstance(genericType)!;
		}

		public static PropertySelector CreateGeneric(Type propertySelectorType, Type innerType, List<PropertySelector> parameters)
		{
			if (propertySelectorType.IsAssignableTo(typeof(PropertySelector)) == false)
			{
				throw new ArgumentException($"The provided type {propertySelectorType.FullName} is not a PropertySelector.");
			}
			var genericType = propertySelectorType.MakeGenericType(innerType);
			return (PropertySelector)Activator.CreateInstance(genericType, parameters)!;
		}
	}
}
