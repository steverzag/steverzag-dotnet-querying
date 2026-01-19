namespace ExpDataWorkerBeta.Feature.PropertySelectors
{
	public abstract record ParameterizedFunctionPropertySelector : PropertySelector
	{
		public List<FunctionPropertySelectorParameters> Overloads { get; set; } = [];
		public List<PropertySelector> Parameters { get; set; } = [];
		public abstract void InitializeOverloads();

		protected ParameterizedFunctionPropertySelector(string PlainValue) : base(PlainValue)
		{
			InitializeOverloads();
		}

		protected ParameterizedFunctionPropertySelector(string PlainValue, List<PropertySelector> parameters) : this(PlainValue)
		{
			if (!Overloads.Select(ov => ov.Parameters.Count).Contains(parameters.Count))
				throw new ArgumentException($"Invalid number of parameters. Any overload for (missing) expects {parameters.Count} parameters.");

			Parameters = parameters;
		}
	}
}
