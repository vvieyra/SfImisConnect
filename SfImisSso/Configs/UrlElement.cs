using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Telerik.Sitefinity.Configuration;

namespace SfImisSso.Configs
{
	public class UrlElement : ConfigElement
	{
		public UrlElement(ConfigElement parent) : base(parent) { }
		[ConfigurationProperty("name", DefaultValue = "", IsRequired = true, IsKey = true)]
		public string Name
		{
			get
			{
				return (string)this["name"];
			}
			set
			{
				this["name"] = value;
			}
		}

		[ConfigurationProperty("Url", DefaultValue = "", IsRequired = true)]
		public string Url
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
	}
}
