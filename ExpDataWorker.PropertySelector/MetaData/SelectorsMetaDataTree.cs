using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.MetaData
{
	public class SelectorsMetaDataTree(Type selectorType, string description)
	{
		public Type SelectorType { get; } = selectorType;
		public string Description { get; } = description;
		public List<SelectorsMetaDataTree> Selectors { get; set; } = [];
	}
}
