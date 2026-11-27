using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.QueryShaper.Exceptions
{
	public sealed class FilterConditionAndValueNotSupportedException(string message) : Exception(message)
	{
	}
}
