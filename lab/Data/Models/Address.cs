using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.Data.Models
{
	public class Address
	{
		public int Id { get; set; }
		public string City { get; set; } = null!;
		public string State { get; set; } = null!;
		public string ZipCode { get; set; } = null!;
		public string AddressLine { get; set; } = null!;

		public AppPerson Person { get; set; } = null!;
		public int PersonId { get; set; }

	}
}
