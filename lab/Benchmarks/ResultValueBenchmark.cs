using Steverzag.QueryShaper.Filtering;
using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.Benchmark
{
	[MemoryDiagnoser]
	public class SearchValueBenchmark
	{

		//[Benchmark]
		//public byte ValueResultOnByte()
		//{
		//	FilterValue byteValueResult = byte.MaxValue;

		//	byte result = (byte)byteValueResult;

		//	return result;
		//}

		//[Benchmark]
		//public short ValueResultOnShort()
		//{
		//	FilterValue byteValueResult = short.MaxValue;

		//	short result = (short)byteValueResult;

		//	return result;
		//}

		//[Benchmark]
		//public int ValueResultOnInt()
		//{
		//	FilterValue intValueResult = int.MaxValue;

		//	int result = (int)intValueResult;

		//	return result;
		//}

		//[Benchmark]
		//public long ValueResultOnLong()
		//{
		//	FilterValue longValueResult = long.MaxValue;

		//	long result = (long)longValueResult;

		//	return result;
		//}

		[Benchmark]
		public float ValueResultOnFloat()
		{
			FilterValue floatValueResult = float.MaxValue;

			float result = (float)floatValueResult;

			return result;
		}

		[Benchmark]
		public double ValueResultOnDouble()
		{
			FilterValue doubleValueResult = double.MaxValue;

			double result = (double)doubleValueResult;

			return result;
		}

		[Benchmark]
		public decimal ValueResultOnDecimal()
		{
			FilterValue decimalValueResult = decimal.MaxValue;

			decimal result = (decimal)decimalValueResult;

			return result;
		}

		//[Benchmark]
		//public int ValueResultOnInt2()
		//{
		//	int result = int.MaxValue;

		//	return result;
		//}
	}
}
