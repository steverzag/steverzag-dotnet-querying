using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.NullSelectors;

[SelectorToken(SelectorDefaults.Coalesce)]
internal record CoalesceSelector<T> : FunctionPropertySelector<T>
{
	public CoalesceSelector() : base(SelectorDefaults.Coalesce) { }
	public CoalesceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.Coalesce, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => CoalesceSelector.Table;
}

[SelectorToken(SelectorDefaults.Coalesce)]
internal record CoalesceSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Any], (t, a) => Implementations.ResolveCoalesceType([t, .. a]),
			p => target => target.CreateCoalesceExpression(p))
		{ Rest = ArgumentConstraint.Any },
	];

	public CoalesceSelector() : base(SelectorDefaults.Coalesce) { }
	public CoalesceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.Coalesce, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
