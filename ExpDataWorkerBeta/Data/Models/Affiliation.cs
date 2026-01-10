using ExpDataWorkerBeta.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Data.Models
{
	public class Affiliation
	{
		public int Id { get; set; }
		public string Name { get; set; } = null!;
		public string Description { get; set; } = null!;
		public DateOnly CreatedOn { get; set; }
		public int? NumberOfMembers { get; set; }
		public ICollection<WorkingHours> WorkingHours { get; set; }
		public ICollection<AffiliationPersonMap> AffiliatedPeople { get; set; }

	}
}
