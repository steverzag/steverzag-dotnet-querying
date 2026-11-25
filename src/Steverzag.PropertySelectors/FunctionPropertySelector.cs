namespace Steverzag.PropertySelectors
{
	internal interface IPipeableFunctionSelector
	{
		bool IsPiped { get; set; }
	}

	internal sealed record PipedValueSelector : PropertySelector
	{
		public string PipedText { get; }

		public PipedValueSelector(Type? pipedType, string pipedText)
		{
			SelectorType = pipedType;
			PipedText = pipedText;
		}
	}

	public abstract record FunctionPropertySelector<T> : PropertySelector<T>, IPipeableFunctionSelector
	{
		public bool IsPiped { get; internal set; }
		bool IPipeableFunctionSelector.IsPiped { get => IsPiped; set => IsPiped = value; }
		public List<FunctionPropertySelectorOverload> Overloads { get; set; } = [];
		public IReadOnlyList<PropertySelector> Parameters { get; init; } = [];
		public abstract void InitializeOverloads();

		protected FunctionPropertySelector(string PlainValue, List<PropertySelector>? parameters = null) : base()
		{
			Parameters = parameters ?? [];
			InitializeOverloads();
			this.PlainValue = $"{PlainValue}({string.Join(",", Parameters.Select(p => p.PlainValue))})";

			var matchingOverload = Overloads.FirstOrDefault(ov => ov.Parameters.Count == Parameters.Count)
				?? throw new ArgumentException(FunctionSelectorFormatter.ArgumentCountMessage(PlainValue, Parameters));

			PropertyAccess = matchingOverload.Behavior!; // subject-less overloads receive a null target and ignore it
		}

		protected override string SegmentToString() => FunctionSelectorFormatter.Format(PlainValue, Parameters);
	}
	public abstract record FunctionPropertySelector : PropertySelector, IPipeableFunctionSelector
	{
		public bool IsPiped { get; internal set; }
		bool IPipeableFunctionSelector.IsPiped { get => IsPiped; set => IsPiped = value; }
		public List<FunctionPropertySelectorOverload> Overloads { get; set; } = [];
		public IReadOnlyList<PropertySelector> Parameters { get; init; } = [];
		public abstract void InitializeOverloads();

		//protected FunctionPropertySelector(string PlainValue) : base(PlainValue)
		//{
		//	InitializeOverloads();

		//	//TODO: Overloads are now validated based on count, should be on type. Need to find better way.
		//	var matchingOverload = Overloads.FirstOrDefault(ov => ov.Parameters.Count == Parameters.Count)
		//		?? throw new ArgumentException($"Invalid number of parameters. Any overload for {PlainValue} expects {Overloads.Select(ov => ov.Parameters.Count)} parameters."); ;

		//	PropertyAccess = matchingOverload.Behavior!; // subject-less overloads receive a null target and ignore it
		//}

		protected FunctionPropertySelector(string PlainValue, List<PropertySelector>? parameters = null) : base()
		{
			Parameters = parameters ?? [];
			InitializeOverloads();
			this.PlainValue = $"{PlainValue}({string.Join(",", Parameters.Select(p => p.PlainValue))})";

			var matchingOverload = Overloads.FirstOrDefault(ov => ov.Parameters.Count == Parameters.Count)
				?? throw new ArgumentException(FunctionSelectorFormatter.ArgumentCountMessage(PlainValue, Parameters));

			PropertyAccess = matchingOverload.Behavior!; // subject-less overloads receive a null target and ignore it
		}

		protected override string SegmentToString() => FunctionSelectorFormatter.Format(PlainValue, Parameters);
	}

	internal static class FunctionSelectorFormatter
	{
		internal static string Format(string plainValue, IReadOnlyList<PropertySelector> parameters)
		{
			var argumentsStart = plainValue.IndexOf('(');
			var name = argumentsStart < 0 ? plainValue : plainValue[..argumentsStart];

			if (parameters.Count > 0 && parameters[0] is PipedValueSelector)
				parameters = [.. parameters.Skip(1)];

			return parameters.Count == 0
				? name
				: $"{name}({string.Join(", ", parameters.Select(p => p.ToString()))})";
		}

		internal static string ArgumentCountMessage(string token, IReadOnlyList<PropertySelector> parameters)
		{
			var written = Format(token, parameters);

			if (parameters.Count == 0 || parameters[0] is not PipedValueSelector piped)
				return $"{token} can't be called with {DescribeCount(parameters.Count)}: '{written}'.";

			return $"{token} can't be called with {DescribeCount(parameters.Count - 1)} besides the piped value: " +
				$"'{piped.PipedText}{SelectorDefaults.PropertyDelimiter}{written}'. The piped value is its first argument, " +
				$"so '{piped.PipedText}{SelectorDefaults.PropertyDelimiter}{token}(…)' means '{token}({piped.PipedText}, …)'.";
		}

		private static string DescribeCount(int count) => count switch
		{
			0 => "no arguments",
			1 => "1 argument",
			_ => $"{count} arguments",
		};
	}
}
