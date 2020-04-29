using nHydrate.Generator.Common.GeneratorFramework;
using nHydrate.Generator.Common.EventArgs;

namespace nHydrate.Generator.PostgresInstaller.ProjectItemGenerators.DatabaseUpgrade
{
    [GeneratorItem("UpgradeVersioned", typeof(PostgresDatabaseProjectGenerator))]
    public class UpgradeVersionedGenerator : BaseDbScriptGenerator
    {
        #region Class Members

        private const string PARENT_ITEM_NAME = @"2_Upgrade Scripts";

        #endregion

        #region Overrides

        public override int FileCount => 1;

        public override void Generate()
        {
            var template = new UpgradeVersionedTemplate(_model);
            var fullFileName = template.FileName;
            var eventArgs = new ProjectItemGeneratedEventArgs(fullFileName, template.FileContent, ProjectName, PARENT_ITEM_NAME, ProjectItemType.Folder, this, false);
            eventArgs.Properties.Add("BuildAction", 3);
            OnProjectItemGenerated(this, eventArgs);
            var gcEventArgs = new ProjectItemGenerationCompleteEventArgs(this);
            OnGenerationComplete(this, gcEventArgs);
        }

        #endregion

    }
}
