using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringConcat, Piped = PipedForm.FirstArgument)]
internal record StringConcatSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(string);
	public StringConcatSelector() : base(SelectorDefaults.StringConcat) { }
	public StringConcatSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringConcat, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringStaticFunction(MethodInfos.StringConcat_2_MethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
		else if (Parameters.Count == 3)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) }, { 2, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringStaticFunction(MethodInfos.StringConcat_3_MethodInfo, Parameters[0], Parameters[1], Parameters[2]));
			Overloads.Add(selectorOverload);
		}
		else if (Parameters.Count == 4)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) }, { 2, typeof(PropertySelector) }, { 3, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringStaticFunction(MethodInfos.StringConcat_4_MethodInfo, Parameters[0], Parameters[1], Parameters[2], Parameters[3]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringConcat, Piped = PipedForm.FirstArgument)]
internal record StringConcatSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(string);
	public StringConcatSelector() : base(SelectorDefaults.StringConcat) { }
	public StringConcatSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringConcat, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringStaticFunction(MethodInfos.StringConcat_2_MethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
		else if (Parameters.Count == 3)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) }, { 2, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringStaticFunction(MethodInfos.StringConcat_3_MethodInfo, Parameters[0], Parameters[1], Parameters[2]));
			Overloads.Add(selectorOverload);
		}
		else if (Parameters.Count == 4)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) }, { 2, typeof(PropertySelector) }, { 3, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringStaticFunction(MethodInfos.StringConcat_4_MethodInfo, Parameters[0], Parameters[1], Parameters[2], Parameters[3]));
			Overloads.Add(selectorOverload);
		}
	}
}
