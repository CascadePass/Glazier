using System.Windows;

namespace CascadePass.Glazier.UI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class WorkspaceViewModelTests
    {
        [TestMethod]
        public void Constructor_ObjectsAreNotNull()
        {
            WorkspaceViewModel workspaceViewModel = new();

            Assert.IsNotNull(workspaceViewModel.GlazierViewModel, "GlazierViewModel should not be null after construction.");
            Assert.IsNotNull(workspaceViewModel.Settings, "Settings should not be null after construction.");
            Assert.IsNotNull(workspaceViewModel.SettingsViewModel, "SettingsViewModel should not be null after construction.");
            Assert.IsNotNull(workspaceViewModel.OriginalImageColumnWidth, "OriginalImageColumnWidth should not be null after construction.");
            Assert.IsNotNull(workspaceViewModel.CurrentFont, "CurrentFont should not be null after construction.");

            Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceViewModel.SettingsFilename));
        }

        [TestMethod]
        public void GetSettings_ReturnsCachedSettings()
        {
            Settings testSettings = new();
            WorkspaceViewModel viewModel = new() { Settings = testSettings };

            Assert.AreSame(testSettings, viewModel.GetSettings(), "GetSettings should return the cached settings instance.");
        }

        [TestMethod]
        public void ChangingImageColumnWidth_ShouldUpdateVisibility()
        {
            var viewModel = new WorkspaceViewModel();

            viewModel.OriginalImageColumnWidth = new GridLength(10);

            Assert.IsTrue(viewModel.IsSettingsPageVisible);
        }

    }
}
