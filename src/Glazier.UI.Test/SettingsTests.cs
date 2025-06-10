using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Media;

namespace CascadePass.Glazier.UI.Tests
{
    [TestClass]
    public class SettingsTests
    {
        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        [TestMethod]
        public void GetDefault_IsNotNull()
        {
            Assert.IsNotNull(Settings.GetDefault());
        }

        [TestMethod]
        public void GetDefault_ValuesMakeSense()
        {
            var defaultSettings = Settings.GetDefault();

            // Font family is bound to MainWindow.FontFamily, should be a real font
            Assert.IsNotNull(defaultSettings);
            Assert.IsNotNull(defaultSettings.FontFamily);
            Assert.IsTrue(Fonts.SystemFontFamilies.Any(f => f.FamilyNames.Values.Contains(defaultSettings.FontFamily)));

            // Font size should be reasonable
            Assert.IsTrue(defaultSettings.FontSize >= 7, "Font is too small to read comfortably.");
            Assert.IsTrue(defaultSettings.FontSize <= 15, "Font is too big.");

            // Theme should default to None (auto-detect)
            Assert.AreEqual(GlazierTheme.None, defaultSettings.Theme, "Theme should default to None (auto-detect).");

            // Background brush should be set (not blank)
            Assert.IsNotNull(defaultSettings.BackgroundBrushKey);

            // Resizing options should not be empty
            Assert.IsFalse(defaultSettings.ResizingOptions is null || defaultSettings.ResizingOptions.Count == 0, "No resizing options");
        }

        [TestMethod]
        public void PropertyChange_Should_UpdateValue()
        {
            var settings = new Settings
            {
                FontSize = 16
            };

            Assert.AreEqual(16, settings.FontSize);
        }

        [TestMethod]
        public void JsonSerialization_Should_PreserveValues()
        {
            var original = Settings.GetDefault();
            string json = original.ToJson(jsonOptions);
            var deserialized = Settings.FromJson(json, jsonOptions);

            Assert.AreEqual(original.FontSize, deserialized.FontSize);
            Assert.AreEqual(original.Theme, deserialized.Theme);
            Assert.AreEqual(original.GlazeMethod, deserialized.GlazeMethod);
            Assert.AreEqual(original.ShowOriginalImage, deserialized.ShowOriginalImage);
            Assert.AreEqual(original.UseAnimation, deserialized.UseAnimation);
            Assert.AreEqual(original.ModelFile, deserialized.ModelFile);
            Assert.AreEqual(original.FontFamily, deserialized.FontFamily);
            Assert.AreEqual(original.BackgroundBrushKey, deserialized.BackgroundBrushKey);
            Assert.AreEqual(original.ResizingOptions.Count, deserialized.ResizingOptions.Count);
        }

        [TestMethod]
        public void FileSaveLoad_Should_PersistData()
        {
            string tempFile = Path.GetTempFileName();
            var settings = Settings.GetDefault();
            settings.SaveToFile(tempFile, jsonOptions);

            var loadedSettings = Settings.LoadFromFile(tempFile, jsonOptions);

            Assert.AreEqual(settings.FontSize, loadedSettings.FontSize);
            Assert.AreEqual(settings.Theme, loadedSettings.Theme);
            Assert.AreEqual(settings.GlazeMethod, loadedSettings.GlazeMethod);
            Assert.AreEqual(settings.ShowOriginalImage, loadedSettings.ShowOriginalImage);
            Assert.AreEqual(settings.UseAnimation, loadedSettings.UseAnimation);
            Assert.AreEqual(settings.ModelFile, loadedSettings.ModelFile);
            Assert.AreEqual(settings.FontFamily, loadedSettings.FontFamily);
            Assert.AreEqual(settings.BackgroundBrushKey, loadedSettings.BackgroundBrushKey);
            Assert.AreEqual(settings.ResizingOptions.Count, loadedSettings.ResizingOptions.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void LoadFromFile_Should_ThrowException_If_FileNotExists()
        {
            Settings.LoadFromFile("NonExistentFile.json", jsonOptions);
        }

        [TestMethod]
        public void BackgroundBrushKey_RejectsInvalidBrushKeys()
        {
            var settings = Settings.GetDefault();
            var invalidBrushKey = Guid.NewGuid().ToString();

            settings.BackgroundBrushKey = invalidBrushKey;

            Assert.IsFalse(string.Equals(invalidBrushKey, settings.BackgroundBrushKey));
            Assert.IsTrue(Settings.ValidBrushKeys.Contains(settings.BackgroundBrushKey));
        }

        [TestMethod]
        public void GetDefaultSizes_IsNotNullOrEmpty()
        {
            Assert.IsNotNull(Settings.GetDefaultSizes());
            Assert.IsFalse(Settings.GetDefaultSizes().Count == 0);
        }

        [TestMethod]
        public void GetDefaultSizes_CommonIconSizes()
        {
            Assert.IsNotNull(Settings.GetDefaultSizes());
            Assert.IsFalse(Settings.GetDefaultSizes().Count == 0);

            Assert.IsTrue(Settings.GetDefaultSizes().Any(s => s.Width == 16 && s.Height == 16));
            Assert.IsTrue(Settings.GetDefaultSizes().Any(s => s.Width == 32 && s.Height == 32));
            Assert.IsTrue(Settings.GetDefaultSizes().Any(s => s.Width == 64 && s.Height == 64));
            Assert.IsTrue(Settings.GetDefaultSizes().Any(s => s.Width == 128 && s.Height == 128));
            Assert.IsTrue(Settings.GetDefaultSizes().Any(s => s.Width == 256 && s.Height == 256));
        }

        [TestMethod]
        public void GetApplicationName_ReturnsKnownValue()
        {
            Assert.AreEqual("GlazierTestHost", Settings.GetApplicationName(), "Application name should be 'GlazierTestHost' in unit tests.");
        }

        #region Validation Methods

        [TestMethod]
        public void ValidateFontSize_ShouldClampValuesCorrectly()
        {
            var settingsValidator = new Settings();

            Assert.AreEqual(7, settingsValidator.ValidateFontSize(3), "Values below 7 should be clamped to 7.");
            Assert.AreEqual(32, settingsValidator.ValidateFontSize(40), "Values above 32 should be clamped to 32.");
            Assert.AreEqual(16, settingsValidator.ValidateFontSize(16), "Values within range should remain unchanged.");
        }

        [TestMethod]
        public void ValidateFontFamily_ShouldRejectNullOrEmptyFontNames()
        {
            var settingsValidator = new Settings();

            Assert.AreEqual(Settings.DEFAULT_FONT, settingsValidator.ValidateFontFamily(null));
            Assert.AreEqual(Settings.DEFAULT_FONT, settingsValidator.ValidateFontFamily(string.Empty));
            Assert.AreEqual(Settings.DEFAULT_FONT, settingsValidator.ValidateFontFamily(" "));
            Assert.AreEqual(Settings.DEFAULT_FONT, settingsValidator.ValidateFontFamily("\t"));
            Assert.AreEqual(Settings.DEFAULT_FONT, settingsValidator.ValidateFontFamily("\r"));
            Assert.AreEqual(Settings.DEFAULT_FONT, settingsValidator.ValidateFontFamily("\n"));
            Assert.AreEqual(Settings.DEFAULT_FONT, settingsValidator.ValidateFontFamily(" \t\r\n "));
        }

        [TestMethod]
        public void ValidateBackgroundBrushKey_ShouldRejectInvalidNames()
        {
            var settingsValidator = new Settings();
            string invalidBrushKey = Guid.NewGuid().ToString();

            Assert.AreNotEqual(invalidBrushKey, settingsValidator.ValidateBackgroundBrushKey(invalidBrushKey));
        }

        [TestMethod]
        public void ValidateBackgroundBrushKey_ShouldAcceptValidNames()
        {
            var settingsValidator = new Settings();
            string[] validBrushKeys = ["ContrastyBackgroundBrush", "CrosshatchBrush"];

            foreach(var validBrushKey in validBrushKeys)
            {
                Assert.AreEqual(validBrushKey, settingsValidator.ValidateBackgroundBrushKey(validBrushKey));
            }
        }

        [TestMethod]
        public void ValidateModelFilename_ShouldReturnTrueForExistingFile()
        {
            var settingsValidator = new Settings();
            string existingFile = @"C:\\Windows\\notepad.exe";
            Assert.AreEqual(existingFile, settingsValidator.ValidateModelFilename(existingFile));
        }

        [TestMethod]
        public void ValidateModelFilename_ShouldReturnFalseForNonExistentFile()
        {
            var settingsValidator = new Settings();
            string existingFile = @$"C:\\Windows\\{Guid.NewGuid()}.exe";
            Assert.IsNull(settingsValidator.ValidateModelFilename(existingFile));
        }

        [TestMethod]
        public void ValidateSizeOptions_ShouldReturnListWhenNullIsSupplied()
        {
            var settingsValidator = new Settings();

            Assert.IsNotNull(settingsValidator.ValidateSizeOptions(null));
        }

        #endregion
    }
}
