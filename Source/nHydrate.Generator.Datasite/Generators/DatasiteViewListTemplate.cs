#pragma warning disable 0168
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using nHydrate.Generator;
using nHydrate.Generator.Models;
using System.Collections;
using nHydrate.Generator.Common.Util;
using nHydrate.Generator.ProjectItemGenerators;

namespace nHydrate.Generator.Datasite
{
	class DatasiteViewListTemplate : BaseScriptTemplate
	{
		private StringBuilder sb = new StringBuilder();
		private string _templateLocation = string.Empty;

		#region Constructors
		public DatasiteViewListTemplate(ModelRoot model, string templateLocation)
			: base(model)
		{
			_templateLocation = templateLocation;
		}
		#endregion

		#region BaseClassTemplate overrides
		public override string FileContent
		{
			get
			{
				this.GenerateContent();
				return sb.ToString();
			}
		}

		public override string FileName
		{
			get { return string.Format("views.html"); }
		}
		#endregion

		#region GenerateContent
		private void GenerateContent()
		{
			try
			{
				var fileContent = Helpers.GetFileContent(new EmbeddedResourceName(_templateLocation + ".datasite-view-overview.htm"));

				fileContent = fileContent.Replace("$databasename$", _model.ProjectName);
				fileContent = fileContent.Replace("$pagetitle$", "Tables Documentation");

				var tsb = new StringBuilder();
				tsb.AppendLine("<table class=\"subItem-item\">");
				tsb.AppendLine("<thead>");
				tsb.AppendLine("<tr>");
				tsb.AppendLine("<th>Name</th>");
				tsb.AppendLine("<th>Columns</th>");
				tsb.AppendLine("<th>Description</th>");
				tsb.AppendLine("</tr>");
				tsb.AppendLine("</thead>");

				tsb.AppendLine("<tbody>");
				foreach (var entity in _model.Database.CustomViews.Where(x => x.Generated).OrderBy(x => x.Name))
				{
					tsb.AppendLine("<tr>");
					tsb.AppendLine("<td><a href=\"view." + entity.PascalName + ".html\">" + entity.Name + "</a></td>");
					tsb.AppendLine("<td>" + entity.GeneratedColumns.Count() + "</td>");
					tsb.AppendLine("<td class=\"description\">" + entity.Description + "</td>");
					tsb.AppendLine("</tr>");
				}
				tsb.AppendLine("</tbody>");
				tsb.AppendLine("</table>");
				fileContent = fileContent.Replace("$listtable$", tsb.ToString());

				fileContent = fileContent.Replace("$footertext$", "Powered by nHydrate &copy; " + DateTime.Now.Year);

				sb.Append(fileContent);

			}
			catch (Exception ex)
			{
				throw;
			}
		}
		#endregion
	}
}
