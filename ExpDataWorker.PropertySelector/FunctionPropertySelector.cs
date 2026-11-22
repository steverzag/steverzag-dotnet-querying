namespace ExpDataWorker.PropertySelectors
{
	public abstract record FunctionPropertySelector<T> : PropertySelector<T>
	{
		public List<FunctionPropertySelectorOverload> Overloads { get; set; } = [];
		public IReadOnlyList<PropertySelector> Parameters { get; init; } = [];
		public abstract void InitializeOverloads();

		protected FunctionPropertySelector(string PlainValue, List<PropertySelector>? parameters = null) : base()
		{
			Parameters = parameters ?? [];
			InitializeOverloads();
			this.PlainValue = $"{PlainValue}({string.Join(",", Parameters.Select(p => p.PlainValue))})";

			var matchingOverload = Overloads.FirstOrDefault(ov => ov.Parameters.Count == Parameters.Count)
				?? throw new ArgumentException($"Invalid number of parameters. Any overload for {PlainValue} expects {Overloads.Select(ov => ov.Parameters.Count)} parameters."); ;

			PropertyAccess = matchingOverload.Behavior!; // subject-less overloads receive a null target and ignore it
		}
	}
	public abstract record FunctionPropertySelector : PropertySelector
	{
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
				?? throw new ArgumentException($"Invalid number of parameters. Any overload for {PlainValue} expects {Overloads.Select(ov => ov.Parameters.Count)} parameters."); ;

			PropertyAccess = matchingOverload.Behavior!; // subject-less overloads receive a null target and ignore it
		}
	}
}
