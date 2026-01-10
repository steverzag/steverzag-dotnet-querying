using ExpDataWorkerBeta.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Data.Models
{
	public class AffiliationPersonMap
	{
		public AffiliationType AffiliationType { get; set; }
		public int AffiliationId { get; set; }
		public Affiliation Affiliation { get; set; } = null!;
		public int PersonId { get; set; }
		public AppPerson Person { get; set; } = null!;
		public DateTimeOffset AffiliatedAt { get; set; }
	}
}
