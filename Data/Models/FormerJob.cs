using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.Data.Models
{
	public class FormerJob
	{
		public int Id { get; set; }
		public string CompanyName { get; set; } = null!;
		public double MonthlySalary { get; set; }
		public string Address { get; set; } = null!;
		public DateOnly SinceDate { get; set; }
		public DateOnly? ToDate { get; set; }
		public TimeOnly StartHour { get; set; }
		public TimeOnly? EndHour { get; set; }
		public short NumberOfEmployees { get; set; }
		public int PersonId { get; set; }
		public AppPerson Person { get; set; }
	}
}
