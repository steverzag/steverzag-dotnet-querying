using ExpDataWorker.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.Data.Models
{
	public class Phone
	{
		public int Id { get; set; }
		public string? CountryCode { get; set; }
		public string Number { get; set; } = null!;
		public bool? IsOptedIn { get; set; }
		public PhoneType PhoneType { get; set; }
		public AppPerson Person { get; set; }
		public int PersonId { get; set; }
	}
}
