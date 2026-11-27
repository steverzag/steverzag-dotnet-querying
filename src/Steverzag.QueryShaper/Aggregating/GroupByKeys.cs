using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Steverzag.QueryShaper.Aggregating
{
	/// <summary>
	/// The key of a group built by <see cref="QueryableExtensions.ApplyGrouping"/>: one slot per grouping selector, in
	/// order. Select a key in <c>ApplySelect</c> as <c>Key.GroupKey0</c>, <c>Key.GroupKey1</c>, and so on.
	/// </summary>
	/// <remarks>It is public because the grouped query exposes it, and hidden from IntelliSense because code rarely needs it apart from <see cref="MaxKeys"/>.</remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public sealed class GroupByKeys
	{
		/// <summary>The most grouping selectors a grouping can have: 21.</summary>
		public const int MaxKeys = 21;

		/// <summary>The value of the 1st grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey0 { get; init; }
		/// <summary>The value of the 2nd grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey1 { get; init; }
		/// <summary>The value of the 3rd grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey2 { get; init; }
		/// <summary>The value of the 4th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey3 { get; init; }
		/// <summary>The value of the 5th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey4 { get; init; }
		/// <summary>The value of the 6th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey5 { get; init; }
		/// <summary>The value of the 7th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey6 { get; init; }
		/// <summary>The value of the 8th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey7 { get; init; }
		/// <summary>The value of the 9th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey8 { get; init; }
		/// <summary>The value of the 10th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey9 { get; init; }
		/// <summary>The value of the 11th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey10 { get; init; }
		/// <summary>The value of the 12th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey11 { get; init; }
		/// <summary>The value of the 13th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey12 { get; init; }
		/// <summary>The value of the 14th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey13 { get; init; }
		/// <summary>The value of the 15th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey14 { get; init; }
		/// <summary>The value of the 16th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey15 { get; init; }
		/// <summary>The value of the 17th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey16 { get; init; }
		/// <summary>The value of the 18th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey17 { get; init; }
		/// <summary>The value of the 19th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey18 { get; init; }
		/// <summary>The value of the 20th grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey19 { get; init; }
		/// <summary>The value of the 21st grouping selector, or <see langword="null"/> if there are fewer.</summary>
		public object? GroupKey20 { get; init; }
	}
}
