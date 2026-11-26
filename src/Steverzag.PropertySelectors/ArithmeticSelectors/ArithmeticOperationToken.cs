using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors.ArithmeticSelectors
{
	internal record ArithmeticOperationToken(ArithmeticOperationTokenType Type, string Value, int Position, bool Synthetic = false);
}
