namespace Steverzag.QueryShaper.Filtering
{
	/// <summary>The kind of value a <see cref="FilterValue"/> holds.</summary>
	public enum FilterValueKind
	{
		/// <summary>A <see cref="bool"/>.</summary>
		Bool,
		/// <summary>An integer: a <see cref="byte"/>, <see cref="short"/>, <see cref="int"/> or <see cref="long"/>.</summary>
		Integer,
		/// <summary>A <see cref="double"/> or <see cref="float"/>.</summary>
		Double,
		/// <summary>A <see cref="decimal"/>.</summary>
		Decimal,
		/// <summary>A <see cref="char"/>.</summary>
		Char,
		/// <summary>A <see cref="string"/>. Dates, times, <see cref="System.Guid"/>s and characters sent as JSON text arrive as this kind.</summary>
		String,
		/// <summary>A <see cref="System.DateTime"/>.</summary>
		DateTime,
		/// <summary>A <see cref="System.DateOnly"/>.</summary>
		DateOnly,
		/// <summary>A <see cref="System.TimeOnly"/>.</summary>
		TimeOnly,
		/// <summary>A <see cref="System.DateTimeOffset"/>.</summary>
		DateTimeOffset,
		/// <summary>A <see cref="System.TimeSpan"/>.</summary>
		TimeSpan,
		/// <summary>A <see cref="System.Guid"/>.</summary>
		Guid,
		/// <summary>A list of values, for <c>In</c>, <c>NotIn</c> and the collection conditions.</summary>
		Enumerable,
	}
}
