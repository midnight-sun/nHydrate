#region Copyright (c) 2006-2018 nHydrate.org, All Rights Reserved
// -------------------------------------------------------------------------- *
//                           NHYDRATE.ORG                                     *
//              Copyright (c) 2006-2018 All Rights reserved                   *
//                                                                            *
//                                                                            *
// Permission is hereby granted, free of charge, to any person obtaining a    *
// copy of this software and associated documentation files (the "Software"), *
// to deal in the Software without restriction, including without limitation  *
// the rights to use, copy, modify, merge, publish, distribute, sublicense,   *
// and/or sell copies of the Software, and to permit persons to whom the      *
// Software is furnished to do so, subject to the following conditions:       *
//                                                                            *
// The above copyright notice and this permission notice shall be included    *
// in all copies or substantial portions of the Software.                     *
//                                                                            *
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,            *
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES            *
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  *
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY       *
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,       *
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE          *
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                     *
// -------------------------------------------------------------------------- *
#endregion
using System.Linq;
using nHydrate.Generator.Common.EventArgs;
using nHydrate.Generator.Common.GeneratorFramework;
using nHydrate.Generator.ProjectItemGenerators;
using System.Collections.Generic;
using nHydrate.Generator.Models;

namespace nHydrate.Generator.EFDAL.Interfaces.Generators
{
	[GeneratorItem("AuditEntityGeneratedGenerator", typeof(AuditEntityExtenderGenerator))]
	public class AuditEntityGeneratedGenerator : BaseClassGenerator
	{
		#region Class Members

		private const string RELATIVE_OUTPUT_LOCATION = @"\Audit\";

		#endregion

		#region Overrides

		public override int FileCount
		{
			get { return GetList().Count; }
		}

		private List<Table> GetList()
		{
			return _model.Database.Tables
				.Where(x => x.Generated && !x.AssociativeTable && (x.TypedTable == TypedTableConstants.None) && x.AllowAuditTracking)
				.OrderBy(x => x.Name)
				.ToList();
		}

		public override void Generate()
		{
			foreach (var table in GetList())
			{
				var template = new AuditEntityGeneratedTemplate(_model, table);
				var fullParentName = RELATIVE_OUTPUT_LOCATION + template.ParentItemName;
				var eventArgs = new ProjectItemGeneratedEventArgs(template.FileName, template.FileContent, ProjectName, fullParentName, this, true);
				OnProjectItemGenerated(this, eventArgs);
			}

			//Process deleted items
			foreach (var name in _model.RemovedTables)
			{
				var fullFileName = RELATIVE_OUTPUT_LOCATION + string.Format("I{0}Audit.Generated.cs", name);
				var eventArgs = new ProjectItemDeletedEventArgs(fullFileName, ProjectName, this);
				OnProjectItemDeleted(this, eventArgs);
			}

			//Process tables that are not longer audited
			foreach (var name in _model.Database.Tables.Where(x => !x.AllowAuditTracking))
			{
				var fullFileName = RELATIVE_OUTPUT_LOCATION + string.Format("I{0}Audit.Generated.cs", name);
				var eventArgs = new ProjectItemDeletedEventArgs(fullFileName, ProjectName, this);
				OnProjectItemDeleted(this, eventArgs);
			}

			var gcEventArgs = new ProjectItemGenerationCompleteEventArgs(this);
			OnGenerationComplete(this, gcEventArgs);
		}

		public override string LocalNamespaceExtension
		{
			get { return EFDALInterfaceProjectGenerator.NamespaceExtension; }
		}

		#endregion

	}
}

