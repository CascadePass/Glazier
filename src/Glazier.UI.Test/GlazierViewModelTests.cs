using System;
using System.Linq;
using System.Windows.Input;

namespace CascadePass.Glazier.UI.Tests
{
    [TestClass]
    public class GlazierViewModelTests
    {
        #region Constructor

        [TestMethod]
        public void Constructor_ImageGlazier_NotNull()
        {
            var viewModel = new GlazierViewModel();
            Assert.IsNotNull(viewModel.ImageGlazier, "ImageGlazier should not be null");
        }

        [TestMethod]
        public void Constructor_ImageColors_NotNull()
        {
            var viewModel = new GlazierViewModel();
            Assert.IsNotNull(viewModel.ImageColors, "ImageColors should not be null");
        }

        [TestMethod]
        public void Constructor_FileDialogProvider_NotNull()
        {
            var viewModel = new GlazierViewModel();
            Assert.IsNotNull(viewModel.FileDialogProvider, "FileDialogProvider should not be null");
        }

        #endregion

        #region Commands (Backing Fields)

        [TestMethod]
        public void Commands_Have_Unique_BackingFields()
        {
            var viewModel = new GlazierViewModel();

            // Get the fields:
            var fields = typeof(GlazierViewModel)
                .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Where(f => f.FieldType == typeof(ICommand))
                .ToList();

            // Ensure each field references a unique instance
            var fieldValues = fields.Select(f => f.GetValue(viewModel)).ToList();

            // Check for duplicates
            Assert.AreEqual(fieldValues.Count, fieldValues.Distinct().Count(), "Some ICommand properties share the same backing field!");
        }

        #endregion

        #region Command Implementations

        #region BrowseForImageFileImplementation

        [TestMethod]
        public void BrowseForImageFileImplementation_Open()
        {
            var dialog = new MockFileDialogProvider { SelectedFilePath = "C:\\TestImage.png" };
            var viewModel = new GlazierViewModel { FileDialogProvider = dialog };

            viewModel.BrowseForImageFileImplementation();

            Assert.AreSame(viewModel.SourceFilename, dialog.SelectedFilePath);
        }

        [TestMethod]
        public void BrowseForImageFileImplementation_Cancel()
        {
            var dialog = new MockFileDialogProvider { SelectedFilePath = null };
            var viewModel = new GlazierViewModel { FileDialogProvider = dialog };
            var originalSourceFilename = viewModel.SourceFilename;

            viewModel.BrowseForImageFileImplementation();

            Assert.AreSame(viewModel.SourceFilename, originalSourceFilename);
        }

        #endregion

        #endregion

        #region IsSourceFilenameValid

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        [DataRow("\r")]
        [DataRow("\n")]
        [DataRow("\r\n")]
        [DataRow(" ")]
        [DataRow(null)]
        public void IsSourceFilenameValid_EmptyString(string filename)
        {
            var viewModel = new GlazierViewModel();

            Assert.IsFalse(viewModel.IsSourceFilenameValid, "IsSourceFilenameValid should be false for empty or whitespace filenames");
        }

        [TestMethod]
        [DataRow("AE9C63BB-14DE-434F-9D60-79E12ED37C3E")]
        [DataRow("675657E3-D4A1-4605-B7AF-B9AE94E39652.png")]
        [DataRow("C:\\8B0F31B7-656E-4D9F-A8B7-475DC844C754")]
        public void IsSourceFilenameValid_DoesNotExist(string filename)
        {
            var viewModel = new GlazierViewModel();

            Assert.IsFalse(viewModel.IsSourceFilenameValid, "IsSourceFilenameValid should be false for empty or whitespace filenames");
        }

        #endregion
    }
}
