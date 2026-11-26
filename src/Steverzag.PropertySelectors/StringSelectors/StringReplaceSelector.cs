using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringReplace, Category = FunctionCategory.String)]
internal record StringReplaceSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringReplaceSelector() : base(SelectorDefaults.StringReplace) { }
	public StringReplaceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringReplace, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringReplaceSelector.Table;
}

[SelectorToken(SelectorDefaults.StringReplace, Category = FunctionCategory.String)]
internal record StringReplaceSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String, ArgumentConstraint.String], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringInstanceFunction(MethodInfos.StringReplaceMethodInfo, p[0], p[1]))
		{ Target = ArgumentConstraint.String },
	];

	public StringReplaceSelector() : base(SelectorDefaults.StringReplace) { }
	public StringReplaceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringReplace, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
