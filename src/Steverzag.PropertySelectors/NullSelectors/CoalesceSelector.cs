using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.NullSelectors;

[SelectorToken(SelectorDefaults.Coalesce, Category = FunctionCategory.Null)]
internal record CoalesceSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public CoalesceSelector() : base(SelectorDefaults.Coalesce) { }
	public CoalesceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.Coalesce, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => CoalesceSelector.Table;
}

[SelectorToken(SelectorDefaults.Coalesce, Category = FunctionCategory.Null)]
internal record CoalesceSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Any], (t, a) => Implementations.ResolveCoalesceType([t, .. a]),
			p => target => target.CreateCoalesceExpression(p))
		{ Rest = ArgumentConstraint.Any },
	];

	public CoalesceSelector() : base(SelectorDefaults.Coalesce) { }
	public CoalesceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.Coalesce, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
