using ExpDataWorker.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.Data.Models
{
	public class AppPerson
	{
		public int Id { get; set; }
		public Gender Gender { get; set; }
		public string Name { get; set; } = null!;
		public string? JobTitle { get; set; }
		public string? Email { get; set; }
		public DateTime DateOfBirth { get; set; }
		public Address? Address { get; set; }
		public MaritalStatus? MaritalStatus { get; set; }
		public ICollection<Phone> Phones { get; set; } = [];
		public ICollection<AffiliationPersonMap> Affiliations { get; set; } = [];
		public TaxInfo? TaxInfo { get; set; }
		public FormerJob? Job { get; set; }

	}
}
