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
		private readonly Type? _declaredType;

		public bool IsPiped { get; internal set; }
		bool IPipeableFunctionSelector.IsPiped { get => IsPiped; set => IsPiped = value; }
		public List<FunctionPropertySelectorOverload> Overloads { get; set; } = [];
		public IReadOnlyList<PropertySelector> Parameters { get; init; } = [];
		public FunctionSignature? Signature { get; }
		protected virtual IReadOnlyList<FunctionSignature>? Signatures => null;
		public virtual void InitializeOverloads() { }

		public override Type? SelectorType => Signatures is null ? base.SelectorType : _declaredType;

		protected FunctionPropertySelector(string PlainValue, List<PropertySelector>? parameters = null) : base()
		{
			Parameters = parameters ?? [];
			this.PlainValue = $"{PlainValue}({string.Join(",", Parameters.Select(p => p.PlainValue))})";

			if (Signatures is { } signatures)
			{
				var call = FunctionCall.Resolve(PlainValue, signatures, Parameters, typeof(T), GetType());
				Signature = call.Signature;
				_declaredType = call.DeclaredType;
				PropertyAccess = call.PropertyAccess;
				if (Signature is not null)
					Overloads.Add(FunctionSelectorFormatter.ResolvedOverload(Parameters.Count, call.PropertyAccess));
				return;
			}

			InitializeOverloads();

			var matchingOverload = Overloads.FirstOrDefault(ov => ov.Parameters.Count == Parameters.Count)
				?? throw new ArgumentException(FunctionSelectorFormatter.ArgumentCountMessage(PlainValue, Parameters));

			PropertyAccess = matchingOverload.Behavior!; // subject-less overloads receive a null target and ignore it
		}

		protected override string SegmentToString() => FunctionSelectorFormatter.Format(PlainValue, Parameters);
	}

	public abstract record FunctionPropertySelector : PropertySelector, IPipeableFunctionSelector
	{
		private readonly Type? _declaredType;

		public bool IsPiped { get; internal set; }
		bool IPipeableFunctionSelector.IsPiped { get => IsPiped; set => IsPiped = value; }
		public List<FunctionPropertySelectorOverload> Overloads { get; set; } = [];
		public IReadOnlyList<PropertySelector> Parameters { get; init; } = [];
		public FunctionSignature? Signature { get; }
		protected virtual IReadOnlyList<FunctionSignature>? Signatures => null;
		public virtual void InitializeOverloads() { }

		public override Type? SelectorType => Signatures is null ? base.SelectorType : _declaredType;

		protected FunctionPropertySelector(string PlainValue, List<PropertySelector>? parameters = null) : base()
		{
			Parameters = parameters ?? [];
			this.PlainValue = $"{PlainValue}({string.Join(",", Parameters.Select(p => p.PlainValue))})";

			if (Signatures is { } signatures)
			{
				var call = FunctionCall.Resolve(PlainValue, signatures, Parameters, null, GetType());
				Signature = call.Signature;
				_declaredType = call.DeclaredType;
				PropertyAccess = call.PropertyAccess;
				if (Signature is not null)
					Overloads.Add(FunctionSelectorFormatter.ResolvedOverload(Parameters.Count, call.PropertyAccess));
				return;
			}

			InitializeOverloads();

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

		internal static FunctionPropertySelectorOverload ResolvedOverload(int parameterCount, Func<System.Linq.Expressions.Expression?, System.Linq.Expressions.Expression> behavior)
		{
			var parameters = new SortedList<int, Type>(parameterCount);
			for (var i = 0; i < parameterCount; i++)
				parameters.Add(i, typeof(PropertySelector));
			return new FunctionPropertySelectorOverload(parameters, behavior);
		}

		internal static string ArgumentCountMessage(string token, IReadOnlyList<PropertySelector> parameters, IReadOnlyList<FunctionSignature>? signatures = null)
		{
			var written = Format(token, parameters);
			var piped = parameters.Count > 0 ? parameters[0] as PipedValueSelector : null;
			var accepted = signatures is null ? null : DescribeAccepted(signatures, piped is null ? 0 : 1);

			if (piped is null)
				return $"{token} can't be called with {DescribeCount(parameters.Count)}: '{written}'." +
					(accepted is null ? "" : $" It takes {accepted}.");

			return $"{token} can't be called with {DescribeCount(parameters.Count - 1)} besides the piped value: " +
				$"'{piped.PipedText}{SelectorDefaults.PropertyDelimiter}{written}'." +
				(accepted is null ? "" : $" It takes {accepted} besides the piped value.") +
				$" The piped value is its first argument, " +
				$"so '{piped.PipedText}{SelectorDefaults.PropertyDelimiter}{token}(…)' means '{token}({piped.PipedText}, …)'.";
		}

		private static string? DescribeAccepted(IReadOnlyList<FunctionSignature> signatures, int offset)
		{
			var fixedCounts = signatures.Where(s => s.Rest is null).Select(s => s.Parameters.Count - offset).Where(c => c >= 0).Distinct().Order().ToList();
			var variadicMinimum = signatures.Where(s => s.Rest is not null).Select(s => (int?)Math.Max(0, s.Parameters.Count - offset)).Min();

			var parts = fixedCounts.Select(c => c.ToString()).ToList();
			if (variadicMinimum is { } minimum)
				parts.Add($"{minimum} or more");

			if (parts.Count == 0)
				return null;
			if (variadicMinimum is null && fixedCounts is [0])
				return "no arguments";

			var list = parts.Count == 1 ? parts[0] : $"{string.Join(", ", parts[..^1])} or {parts[^1]}";
			return variadicMinimum is null && fixedCounts is [1] ? "1 argument" : $"{list} arguments";
		}

		private static string DescribeCount(int count) => count switch
		{
			0 => "no arguments",
			1 => "1 argument",
			_ => $"{count} arguments",
		};
	}
}
