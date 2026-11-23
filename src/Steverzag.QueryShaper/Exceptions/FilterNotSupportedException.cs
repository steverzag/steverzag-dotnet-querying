using Steverzag.QueryShaper.Filtering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.QueryShaper.Exceptions
{
	public class FilterNotSupportedException : Exception
	{
		public FilterNotSupportedException(Type notSupportedType) : base($"Filtering is not supported for type '{notSupportedType.Name}'.") { }
		public FilterNotSupportedException(FilterCondition notSupportedCondition, Type notSupportedType) : base($"Filter condition '{notSupportedCondition}' is not supported for type '{notSupportedType.Name}'.") { }
	}
}
