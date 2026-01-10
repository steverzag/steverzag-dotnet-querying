using BenchmarkDotNet.Attributes;
using ExpDataWorker.Data.Models;
using ExpDataWorkerBeta.Feature.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.Benchmark
{
	[MemoryDiagnoser]
	public class CreateMemberAccessBenchmark
	{

		[Benchmark]
		public Expression CreateMemberAccesorL1V2()
		{
			var parameterExpressions = Expression.Parameter(typeof(AppPerson), name: "x");
			var memberAccess = ExpressionUtils.CreateMemberAccess(parameterExpressions, "MaritalStatus");

			return memberAccess;
		}

		[Benchmark]
		public Expression CreateMemberAccesorL2V2()
		{
			var parameterExpressions = Expression.Parameter(typeof(AppPerson), name: "x");
			var memberAccess = ExpressionUtils.CreateMemberAccess(parameterExpressions, "Phones.Number");

			return memberAccess;
		}

		[Benchmark]
		public Expression CreateMemberAccesorL3V2()
		{
			var parameterExpressions = Expression.Parameter(typeof(AppPerson), name: "x");
			var memberAccess = ExpressionUtils.CreateMemberAccess(parameterExpressions, "Affiliations.Affiliation.WorkingHours");

			return memberAccess;
		}
	}
}
