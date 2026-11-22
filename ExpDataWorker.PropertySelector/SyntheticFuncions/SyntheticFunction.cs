using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.SyntheticFuncions
{
	internal record SyntheticFunction : PropertySelector
	{

		public string MyProperty { get; set; } = string.Empty;
	}
}
