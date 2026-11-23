using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.QueryShaper.Utils
{
	public enum ResultValueKind
	{
		Bool,
		Byte,
		Short,
		Integer,
		Long,
		Float,
		Double,
		Decimal,
		Char,
		String,
		DateTime,
		DateOnly,
		TimeOnly,
		DateTimeOffset,
		TimeSpan,
		Guid,
		Enumerable,
	}
}
