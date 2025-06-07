using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace CascadePass.Glazier.UI.Tests
{
    [TestClass]
    public class ThemeViewModelTests
    {
        #region Constructor Tests

        [TestMethod]
        public void Constructor_Default_InitializesPropertiesToNullOrDefault()
        {
            var vm = new ThemeViewModel();

            Assert.AreEqual(default, vm.Theme);
            Assert.IsNull(vm.Name);
            Assert.IsNull(vm.IconPath);
        }

        [TestMethod]
        public void Constructor_WithThemeAndName_SetsProperties()
        {
            var vm = new ThemeViewModel(GlazierTheme.Dark, "DarkTheme");

            Assert.AreEqual(GlazierTheme.Dark, vm.Theme);
            Assert.AreEqual("DarkTheme", vm.Name);
            Assert.IsNull(vm.IconPath);
        }

        [TestMethod]
        public void Constructor_WithThemeNameAndIcon_SetsAllProperties()
        {
            var vm = new ThemeViewModel(GlazierTheme.Light, "LightTheme", "/icon/path.png");

            Assert.AreEqual(GlazierTheme.Light, vm.Theme);
            Assert.AreEqual("LightTheme", vm.Name);
            Assert.AreEqual("/icon/path.png", vm.IconPath);
        }

        #endregion

        [TestMethod]
        public void Property_Setters_UpdateValues()
        {
            var vm = new ThemeViewModel();

            vm.Theme = GlazierTheme.HighContrast;
            vm.Name = "Contrast";
            vm.IconPath = "/contrast.png";

            Assert.AreEqual(GlazierTheme.HighContrast, vm.Theme);
            Assert.AreEqual("Contrast", vm.Name);
            Assert.AreEqual("/contrast.png", vm.IconPath);
        }

        [TestMethod]
        public void Property_Setters_RaisePropertyChanged()
        {
            var vm = new ThemeViewModel();
            string lastProperty = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) => lastProperty = e.PropertyName;

            vm.Name = "TestName";
            Assert.AreEqual("Name", lastProperty);

            vm.Theme = GlazierTheme.Light;
            Assert.AreEqual("Theme", lastProperty);

            vm.IconPath = "icon.png";
            Assert.AreEqual("IconPath", lastProperty);
        }

        [TestMethod]
        public void GetThemes_ReturnsExpectedThemes()
        {
            var themes = ThemeViewModel.GetThemes();
            Assert.IsInstanceOfType(themes, typeof(ObservableCollection<ThemeViewModel>));
            Assert.IsTrue(themes.Count >= 4);

            Assert.IsTrue(themes.Any(t => t.Theme == GlazierTheme.None));
            Assert.IsTrue(themes.Any(t => t.Theme == GlazierTheme.Light));
            Assert.IsTrue(themes.Any(t => t.Theme == GlazierTheme.Dark));
            Assert.IsTrue(themes.Any(t => t.Theme == GlazierTheme.HighContrast));
        }
    }
}