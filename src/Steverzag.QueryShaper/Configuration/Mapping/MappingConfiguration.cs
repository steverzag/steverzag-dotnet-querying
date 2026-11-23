using System.Linq.Expressions;
using System.Reflection;
using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper.Configuration.Mapping;

public class MappingConfiguration<TIn, TOut>
{
	internal Dictionary<PropertyInfo, PropertySelector> Mappings { get; } = [];

	public MappingConfiguration<TIn, TOut> ForMember<TProp>(
		Expression<Func<TOut, TProp>> destinationMember,
		PropertySelector<TIn> selector)
	{
		// Only a property directly on the destination (d => d.X) can be bound. d => d.Inner.X or d => d.X.Length
		// are member accesses too, but would never be applied, so they are rejected here.
		if (destinationMember.Body is not MemberExpression memberExpr
			|| memberExpr.Member is not PropertyInfo prop
			|| memberExpr.Expression is not ParameterExpression)
			throw new ArgumentException("Destination must be a simple property access.", nameof(destinationMember));

		Mappings[prop] = selector;
		return this;
	}

	internal Expression<Func<TIn, TOut>> CreateMappingExpression()
	{
		var param = Expression.Parameter(typeof(TIn), "src");
		var bindings = new List<MemberBinding>(Mappings.Count);

		foreach (var prop in typeof(TOut).GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (!Mappings.TryGetValue(prop, out var selector) && typeof(TIn).GetProperty(prop.Name) is null)
				continue;

			var valueExpr = ExpressionUtils.CreateMemberAccess(param, selector ?? prop.Name);

			Expression assignExpr = valueExpr.Type != prop.PropertyType
				? Expression.Convert(valueExpr, prop.PropertyType)
				: valueExpr;

			bindings.Add(Expression.Bind(prop, assignExpr));
		}

		var body = Expression.MemberInit(Expression.New(typeof(TOut)), bindings);
		var lambda = Expression.Lambda<Func<TIn, TOut>>(body, param);
		return lambda;
	}
}