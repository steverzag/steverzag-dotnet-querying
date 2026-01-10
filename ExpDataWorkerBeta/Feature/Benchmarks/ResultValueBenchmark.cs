using BenchmarkDotNet.Attributes;
using ExpDataWorkerBeta.Feature.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.Benchmarks
{
	[MemoryDiagnoser]
	public class SearchValueBenchmark
	{

		//[Benchmark]
		//public byte ValueResultOnByte()
		//{
		//	ValueResult2 byteValueResult = byte.MaxValue;

		//	byte result = (byte)byteValueResult;

		//	return result;
		//}

		//[Benchmark]
		//public short ValueResultOnShort()
		//{
		//	ValueResult2 byteValueResult = short.MaxValue;

		//	short result = (short)byteValueResult;

		//	return result;
		//}

		//[Benchmark]
		//public int ValueResultOnInt()
		//{
		//	ValueResult2 intValueResult = int.MaxValue;

		//	int result = (int)intValueResult;

		//	return result;
		//}

		//[Benchmark]
		//public long ValueResultOnLong()
		//{
		//	ValueResult2 longValueResult = long.MaxValue;

		//	long result = (long)longValueResult;

		//	return result;
		//}

		[Benchmark]
		public float ValueResultOnFloat()
		{
			ValueResult2 floatValueResult = float.MaxValue;

			float result = (float)floatValueResult;

			return result;
		}

		[Benchmark]
		public double ValueResultOnDouble()
		{
			ValueResult2 doubleValueResult = double.MaxValue;

			double result = (double)doubleValueResult;

			return result;
		}

		[Benchmark]
		public decimal ValueResultOnDecimal()
		{
			ValueResult2 decimalValueResult = decimal.MaxValue;

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
