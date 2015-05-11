using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SfImisSso.Models
{
	public class ImisResponse
	{
		public bool IsAuthenticated { get; set; }
		public string ImisId { get; set; }
		public string MemberType { get; set; }
		public string Status { get; set; }
		public string FirstName { get; set; }
		public string MiddleName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public ImisResponse() {
		
		}
	}
}
