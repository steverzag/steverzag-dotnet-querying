using System.Text.Json.Serialization;
using Steverzag.QueryShaper.Serialization;

namespace Steverzag.QueryShaper.Filtering
{
	/// <summary>
	/// How a <see cref="FilterNode"/> joins everything before it in its list. Nodes combine strictly left to right, with no
	/// precedence; use a <see cref="GroupNode"/> for parentheses.
	/// </summary>
	[JsonConverter(typeof(NameOnlyEnumConverter<LogicalConnector>))]
	public enum LogicalConnector
	{
		/// <summary>Both what came before and this node must match. The default.</summary>
		And,
		/// <summary>What came before or this node must match.</summary>
		Or
	}
}
