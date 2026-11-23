using ExpDataWorker.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.Data.Models
{
	public class TaxInfo
	{
		public int Id { get; set; }
		public TaxType TaxType { get; set; }
		public double TaxRate { get; set; }
		public string Country { get; set; } = null!;
		public DateOnly LastDeclarationDate { get; set; }
		public decimal LastDeclarationAmmount { get; set; }
		public decimal? EstimatedNextDeclarationAmmount { get; set; }
		public int PersonId { get; set; }
		public AppPerson Person { get; set; }
	}
}
