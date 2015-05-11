using SfImisSso.Configs;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.Data;
using Telerik.Sitefinity.Modules.Pages.Configuration;
using System.Linq;
using SfImisSso.Widgets;
namespace SfImisSso
{
	public class Initializer
	{
		public static void PreApplicationStart()
		{
			Bootstrapper.Initialized += Initializer.Bootstrapper_Initialized;
		}
		private static void Bootstrapper_Initialized(object sender, ExecutedEventArgs e)
		{
			if (e.CommandName == "RegisterRoutes")
			{
				Config.RegisterSection<ImisConfig>();

				ConfigManager configManager = ConfigManager.GetManager();
				var toolboxesConfig = configManager.GetSection<ToolboxesConfig>();
				var pageControls = toolboxesConfig.Toolboxes["PageControls"];
				var section = pageControls.Sections.Where<ToolboxSection>(i => i.Name == "Login").FirstOrDefault();
				if (!section.Tools.Contains("IMISLoginWidget"))
				{
					var toolboxItem = new ToolboxItem(section.Tools)
					{
						Name = "IMISLoginWidget",
						Title = "IMIS Login",
						Description = "",
						ResourceClassId = "",
						CssClass = "",
						ControlType = typeof(ExtLoginControl).AssemblyQualifiedName,
						ModuleName = ""
					};
					section.Tools.Add(toolboxItem);

					configManager.SaveSection(toolboxesConfig);
				}
			}
		}
	}
}
