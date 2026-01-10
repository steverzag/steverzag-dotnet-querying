using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Bogus;
using ExpDataWorker.Data.Enums;
using ExpDataWorker.Data.Models;
using ExpDataWorker.Data;
using ExpDataWorkerBeta.Feature.Aggregating;
using ExpDataWorkerBeta.Feature.Filtering;
using ExpDataWorkerBeta.Feature.Ordering;
using ExpDataWorkerBeta.Feature.Pagination;
using ExpDataWorkerBeta.Feature.PropertySelectors;
using Microsoft.EntityFrameworkCore;

var context = new AppDbContext();

var logicFilters = new List<FilterNode>
{
	//new()
	//{
	//	Filters =
	//	[
	//		new()
	//		{
	//			FilterField = nameof(AppPerson.Email),
	//			SearchValue = null,
	//			FilterCondition = FilterCondition.IsEmpty,
	//		},
	//		new()
	//		{
	//			FilterField = nameof(AppPerson.MaritalStatus),
	//			SearchValue = (int)MaritalStatus.Married,
	//			FilterCondition = FilterCondition.Equals,
	//			FilterLogicOption = LogicalConnector.And,
	//		},
	//	],
	//},
	//new ()
	//{
	//	Filters = [
	//		new()
	//		{
	//			FilterField = string.Join(".", [nameof(AppPerson.Affiliations), nameof(AffiliationPersonMap.Affiliation), nameof(Affiliation.WorkingHours)]),
	//			SearchValue = null,
	//			FilterCondition = FilterCondition.IsNotEmpty,
	//		},
	//		new()
	//		{
	//			FilterField = string.Join(".", [nameof(AppPerson.Phones), nameof(Phone.Number)]),
	//			SearchValue = null,
	//			FilterCondition = FilterCondition.IsEmpty,
	//			FilterLogicOption = LogicalConnector.Or,
	//		}
	//	],

	//	FilterLogicOption = LogicalConnector.Or,
	//},
	//new()
	//{ 
	//	FilterField = string.Join(".", [nameof(AppPerson.TaxInfo), nameof(AppPerson.TaxInfo.EstimatedNextDeclarationAmmount)]),
	//	FilterCondition = FilterCondition.IsNotEmpty,
	//	FilterLogicOption = LogicalConnector.And,
	//},
	new()
	{
		FieldSelector = string.Join(".", [nameof(AppPerson.Phones), nameof(Phone.PhoneType)]),
		SearchValue = new List<object>() { PhoneType.Home, PhoneType.Work },
		FilterCondition = FilterCondition.ContainsAll,
		FilterLogicOption = LogicalConnector.And,
	},
	new()
	{
		FieldSelector = string.Join(".", [nameof(AppPerson.Id)]),
		FilterCondition = FilterCondition.NotIn,
		SearchValue = new List<object>() { 1, 2, 3, 4, 5 },
		FilterLogicOption = LogicalConnector.And,
	},
	new ()
	{
		FieldSelector = nameof(AppPerson.Phones) + "." + nameof(Phone.Id) + "|" + FunctionDefaults.AvgAggregateFunction,
		FilterCondition = FilterCondition.Equals,
		SearchValue = 12044.5,
		FilterLogicOption = LogicalConnector.And,
	}
};

var persons = await context.Persons
	.ApplyOrder([
		new OrderParameters(nameof(AppPerson.Phones) + "." + nameof(Phone.Id) + "|" + FunctionDefaults.SumAggregateFunction, OrderDirection.Descending),
	])
	//.ApplyBaseFilter(
	//	[nameof(AppPerson.DateOfBirth), nameof(AppPerson.Email), nameof(AppPerson.Name), string.Join(".", [nameof(AppPerson.Address), nameof(Address.State)])],
	//	"abel"
	//)
	.ApplyLogicsFilter(logicFilters)
	.PaginateAsync(1, 50);

var a = new List<PhoneType> { PhoneType.Mobile, PhoneType.Home };

var persons2 = await context.Persons
	.Where(p => a.Any(x => p.Phones.Select(phone => phone.PhoneType).Contains(x)))
	.OrderByDescending(p => p.Phones.Select(phone => phone.Id).Average())
	.Skip(0).Take(50)
	.ToListAsync();

var aggregate3 = await context.Persons
	.GroupBy(p => new GroupByKeys
	{
		GroupKey0 = p.Phones.Count()
	})
	.Select(g => new
	{
		g.Key,
		Count = g.Select(y => y.Id).Count()
	}
	)
	.ToListAsync();

var aggregate2 = await context.Persons
	.GroupBy(p => new GroupByKeys
	{
		GroupKey0 = p.Phones.Count()
	})
	.Select(g => new AggregateResult
	(
		new List<GroupedByProperty>
		{
			new (nameof(AppPerson.Phones) + "|" + FunctionDefaults.CountAggregateFunction, g.Key.GroupKey0)
		},
		new List<AggregateValue>
		{
			new (FunctionDefaults.CountAggregateFunction, g.Select(y => y.Id).Count())
		}
	))
	.ToListAsync();

var aggregate = await context.Persons
	.ApplyAggregate(new AggregateParameters
	{
		GroupBySelectors = [nameof(AppPerson.Phones) + "|" + FunctionDefaults.CountAggregateFunction],
		AggregateSelectors = [nameof(AppPerson.Id) + "|" + FunctionDefaults.CountAggregateFunction]
	})
	.ToListAsync();

foreach (var person in persons)
{
	Console.WriteLine($"{person.Id}: {person.Name} - {person.Email} - {person.DateOfBirth}");
}
Console.WriteLine("------------------------------");

foreach (var person in persons2)
{
	Console.WriteLine($"{person.Id}: {person.Name} - {person.Email} - {person.DateOfBirth}");
}


