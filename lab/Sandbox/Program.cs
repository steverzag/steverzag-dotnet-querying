using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Bogus;
using ExpDataWorker.Data;
using ExpDataWorker.Data.Enums;
using ExpDataWorker.Data.Models;
using Steverzag.PropertySelectors;
using Steverzag.PropertySelectors.MetaData;
using Steverzag.QueryShaper.Aggregating;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;
using Steverzag.QueryShaper.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

var context = new AppDbContext();

PropertySelector<AppPerson> ps = SelectorDefaults.MathPower + "(2, 2)";
PropertySelector<AppPerson> ps2 = "2 + 2 * 2";
PropertySelector<AppPerson> ps3 = "\"Hi there\"";
PropertySelector<AppPerson> ps4 = "55";

var propertyExplorer = new PropertiesExplorer<AppPerson>();
var metadataTree = propertyExplorer.ExplorePropertiesTree();
//ps.PlainValue

var s1 = ps.ToString();
var s2 = ps2.ToString();
var s3 = ps3.ToString();
var s4 = ps4.ToString();

var logicFilters = new List<FilterNode>
{
	//new()
	//{
	//	Filters =
	//	[
	//		new()
	//		{
	//			FilterField = nameof(AppPerson.Email),
	//			Value = null,
	//			Condition = FilterCondition.IsEmpty,
	//		},
	//		new()
	//		{
	//			FilterField = nameof(AppPerson.MaritalStatus),
	//			Value = (int)MaritalStatus.Married,
	//			Condition = FilterCondition.Equals,
	//			Connector = LogicalConnector.And,
	//		},
	//	],
	//},
	//new ()
	//{
	//	Filters = [
	//		new()
	//		{
	//			FilterField = string.Join(".", [nameof(AppPerson.Affiliations), nameof(AffiliationPersonMap.Affiliation), nameof(Affiliation.WorkingHours)]),
	//			Value = null,
	//			Condition = FilterCondition.IsNotEmpty,
	//		},
	//		new()
	//		{
	//			FilterField = string.Join(".", [nameof(AppPerson.Phones), nameof(Phone.Number)]),
	//			Value = null,
	//			Condition = FilterCondition.IsEmpty,
	//			Connector = LogicalConnector.Or,
	//		}
	//	],

	//	Connector = LogicalConnector.Or,
	//},
	//new()
	//{ 
	//	FilterField = string.Join(".", [nameof(AppPerson.TaxInfo), nameof(AppPerson.TaxInfo.EstimatedNextDeclarationAmmount)]),
	//	Condition = FilterCondition.IsNotEmpty,
	//	Connector = LogicalConnector.And,
	//},
	new ConditionNode()
	{
		Field = string.Join(".", [nameof(AppPerson.Phones), nameof(Phone.PhoneType)]),
		Value = new List<object>() { PhoneType.Home, PhoneType.Work },
		Condition = FilterCondition.ContainsAll,
		Connector = LogicalConnector.And,
	},
	new ConditionNode()
	{
		Field = string.Join(".", [nameof(AppPerson.Id)]),
		Condition = FilterCondition.NotIn,
		Value = new List<object>() { 1, 2, 3, 4, 5 },
		Connector = LogicalConnector.And,
	},
	new ConditionNode()
	{
		Field = nameof(AppPerson.Phones) + "." + nameof(Phone.Id) + "|" + SelectorDefaults.AvgAggregateFunction,
		Condition = FilterCondition.Equals,
		Value = 12044.5,
		Connector = LogicalConnector.And,
	}
};

var persons = await context.Persons
	.OrderBy(p => p.Id)
	//.ApplyOrder([
	//	new OrderParameters(nameof(AppPerson.Phones) + "." + nameof(Phone.Id) + "|" + SelectorDefaults.SumAggregateFunction, OrderDirection.Descending),
	//])
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

//var aggregate3 = await context.Persons
//	.GroupBy(p => new GroupByKeys
//	{
//		GroupKey0 = p.Phones.Count()
//	})
//	.Select(g => new
//	{
//		g.Key,
//		Count = g.Select(y => y.Id).Count()
//	}
//	)
//	.ToListAsync();

//var aggregate2 = await context.Persons
//	.GroupBy(p => new GroupByKeys
//	{
//		GroupKey0 = p.Phones.Count()
//	})
//	.Select(g => new SelectResult
//	(
//		new List<GroupedByProperty>
//		{
//			new (nameof(AppPerson.Phones) + "|" + SelectorDefaults.CountAggregateFunction, g.Key.GroupKey0)
//		},
//		new List<SelectResult>
//		{
//			new (SelectorDefaults.CountAggregateFunction, g.Select(y => y.Id).Count()),
//			new (SelectorDefaults.ElementLast, g.OrderBy(y => y.Id).Select(y => y.Id).Skip(1).FirstOrDefault())
//		}
//	))
//	.ToListAsync();


var aggregate = await context.Persons.Skip(5)
	.ApplySelect([
		nameof(AppPerson.Id), 
		nameof(AppPerson.Name), 
		nameof(AppPerson.Name) + SelectorDefaults.PropertyDelimiter
			+ SelectorDefaults.StringContains + "(" + SelectorLiteral.Format("Merl") + ")"])
	.PaginateAsync(1, 10);

foreach (var person in persons)
{
	Console.WriteLine($"{person.Id}: {person.Name} - {person.Email} - {person.DateOfBirth}");
}
Console.WriteLine("------------------------------");

foreach (var person in persons2)
{
	Console.WriteLine($"{person.Id}: {person.Name} - {person.Email} - {person.DateOfBirth}");
}


