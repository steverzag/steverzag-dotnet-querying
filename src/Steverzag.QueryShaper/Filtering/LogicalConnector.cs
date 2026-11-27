using System.Text.Json.Serialization;
using Steverzag.QueryShaper.Serialization;

namespace Steverzag.QueryShaper.Filtering
{
	[JsonConverter(typeof(NameOnlyEnumConverter<LogicalConnector>))]
	public enum LogicalConnector
	{
		And,
		Or
	}
}
