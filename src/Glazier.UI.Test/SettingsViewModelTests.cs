using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Windows.Media;

namespace CascadePass.Glazier.UI.Tests
{
    [TestClass]
    public class SettingsViewModelTests
    {
        [TestMethod]
        public void Constructor_Settings_Sizes_NotNull()
        {
            var viewModel = new SettingsViewModel(new Settings());
            Assert.IsNotNull(viewModel.AvailableSizes, "Sizes should not be null");
        }

        [TestMethod]
        public void Constructor_Parameterless_Sizes_NotNull()
        {
            var viewModel = new SettingsViewModel();
            Assert.IsNotNull(viewModel.AvailableSizes, "Sizes should not be null");
        }

        [TestMethod]
        public void SelectedTheme_UpdatesSettingsTheme()
        {
            var settings = new Settings();
            var viewModel = new SettingsViewModel(settings);

            // Defaults might change, so explictly set this to None before testing.
            settings.Theme = GlazierTheme.None;
            viewModel.SelectedTheme = viewModel.AvailableThemes.FirstOrDefault(t => t.Theme == GlazierTheme.None);

            // Update the Selected Theme ...
            viewModel.SelectedTheme = viewModel.AvailableThemes.FirstOrDefault(t => t.Theme == GlazierTheme.Light);
            // ... and the Settings object's theme should be updated to match.
            Assert.AreEqual(viewModel.SelectedTheme.Theme, settings.Theme);
        }

        [TestMethod]
        public void SelectedFont_UpdatesSettingsFont()
        {
            var settings = new Settings();
            var viewModel = new SettingsViewModel(settings);

            // Defaults might change, so explictly set this to None before testing.
            settings.FontFamily = "Helvetica";
            viewModel.SelectedFont = viewModel.AvailableFonts.FirstOrDefault(f => f.Source == settings.FontFamily);

            // Update the Selected font ...
            viewModel.SelectedFont = viewModel.AvailableFonts.FirstOrDefault(f => f.Source == "Times New Roman");
            // ... and the Settings object's font should be updated to match.
            Assert.AreEqual(viewModel.SelectedFont.Source, settings.FontFamily);
        }

        [TestMethod]
        public void SelectedImageBackgroundBrush_UpdatesSettingsBrush()
        {
            var settings = new Settings();
            var viewModel = new SettingsViewModel(settings);

            // Defaults might change, so explictly set this to None before testing.
            settings.BackgroundBrushKey = "CrosshatchBrush";
            viewModel.SelectedImageBackgroundBrush = viewModel.AvailableBackgroundBrushes.FirstOrDefault(b => b.Key == settings.BackgroundBrushKey);

            // Update the Selected brush ...
            viewModel.SelectedImageBackgroundBrush = viewModel.AvailableBackgroundBrushes.First();
            // ... and the Settings object's brush should be updated to match.
            Assert.AreEqual(viewModel.SelectedImageBackgroundBrush.Key, settings.BackgroundBrushKey);
        }

        [TestMethod]
        public void AvailableThemes_ShouldReturnSameInstance_OnMultipleCalls()
        {
            // There was a bug that lead to this unit test.  The property
            // called a static method to generate a list, then the
            // SelectedTheme property was chosen from among the list.
            // However WPF would not pre-select a SelectedItem because
            // the SelectedTheme was not present in the new list that
            // would be generated on subsequent calls.

            var viewModel = new SettingsViewModel();

            var firstCall = viewModel.AvailableThemes;
            var secondCall = viewModel.AvailableThemes;

            Assert.AreSame(firstCall, secondCall, "AvailableThemes should return the same instance every time.");
            Assert.IsTrue(firstCall.SequenceEqual(secondCall), "AvailableThemes should contain the same items.");
        }

        [TestMethod]
        public void IsSettingsPageOpen_BothConstructors_ShouldBeTrue()
        {
            // Settings panel display won't work properly without this

            var viewModelWithSettings = new SettingsViewModel(new Settings());
            var viewModelWithoutSettings = new SettingsViewModel();

            Assert.IsTrue(viewModelWithSettings.IsSettingsPageOpen, "IsSettingsPageOpen should be true after construction with settings.");
            Assert.IsTrue(viewModelWithoutSettings.IsSettingsPageOpen, "IsSettingsPageOpen should be true after parameterless construction.");
        }

        [TestMethod]
        public void AvailableFonts_ShouldMatchSystemList()
        {
            var viewModel = new SettingsViewModel();
            var systemFonts = Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList();
            Assert.IsTrue(viewModel.AvailableFonts.SequenceEqual(systemFonts), "AvailableFonts should match the system font families.");
        }

        [TestMethod]
        public void AvailableBackgroundBrushes_IsNotNullByDefault()
        {
            var viewModel = new SettingsViewModel();
            Assert.IsNotNull(viewModel.AvailableBackgroundBrushes, "AvailableBackgroundBrushes should not be null by default.");
        }

        [TestMethod]
        public void AvailableSizes_IsNotNullByDefault()
        {
            var viewModel = new SettingsViewModel();
            Assert.IsNotNull(viewModel.AvailableSizes, "AvailableSizes should not be null by default.");
        }

        [TestMethod]
        public void CanAddSize()
        {
            var settings = new Settings();
            var viewModel = new SettingsViewModel(settings);

            settings.ResizingOptions.Add(new(100, 100));

            // A previous bug threw an exception when adding a new size
        }

        [TestMethod]
        public void CanRemoveSize()
        {
            var settings = new Settings();
            var viewModel = new SettingsViewModel(settings);

            // Since the settings is new, it can be empty.
            settings.ResizingOptions.Add(new(100, 100));

            settings.ResizingOptions.Remove(settings.ResizingOptions.First());

            // A previous bug threw an exception when removing a new size
        }
    }
}
