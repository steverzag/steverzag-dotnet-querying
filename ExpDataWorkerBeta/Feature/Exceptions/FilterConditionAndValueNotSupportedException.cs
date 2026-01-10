using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.Exceptions
{
	public class FilterConditionAndValueNotSupportedException(string message) : Exception(message)
	{
	}
}
