using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.Data.Models
{
	public class WorkingHours
	{
		public int Id { get; set; }
		public DayOfWeek Day { get; set; }
		public TimeOnly? OpenTime { get; set; }
		public TimeOnly? CloseTime { get; set; }
		public Affiliation Affilation { get; set; }
		public int AffiliationId { get; set; }
	}
}
