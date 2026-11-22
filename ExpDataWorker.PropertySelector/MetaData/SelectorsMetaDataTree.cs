using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.MetaData
{
	public class SelectorsMetaDataTree(Type selectorType, string name, string? description = default)
	{
		public Type SelectorType { get; } = selectorType;
		public string Name { get; } = name;
		public string? Description { get; set; } = description;
		public List<SelectorsMetaDataTree> Selectors { get; set; } = [];
	}
}
