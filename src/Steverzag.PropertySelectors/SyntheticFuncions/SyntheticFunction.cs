using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors.SyntheticFuncions
{
	internal record SyntheticFunction : PropertySelector
	{

		public string MyProperty { get; set; } = string.Empty;
	}
}
