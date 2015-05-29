using System.Collections.Specialized;
using System.Configuration;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.Localization;

namespace SfImisSso.Configs
{
	[ObjectInfo(Title = "IMIS Configuration")]
	public class ImisConfig : ConfigSection
	{
		[ObjectInfo(Title = "Url", Description = "Url to submit authorization request to.")]
		[ConfigurationProperty("Url", DefaultValue = "http://www.google.com/api/auth")]
		public string AuthUrl
		{
			get
			{
				return (string)this["Url"];
			}
			set
			{
				this["Url"] = value;
			}
		}
		[ObjectInfo(Title="Domain Url",Description="Domain for Cookie. Must have period prior so it remains a domain cookie.")]
		[ConfigurationProperty("DomainUrl", DefaultValue=".google.com")]
		public string DomainUrl
		{
			get
			{
				return (string)this["DomainUrl"];
			}
			set
			{
				this["DomainUrl"] = value;
			}
		}
	}
}