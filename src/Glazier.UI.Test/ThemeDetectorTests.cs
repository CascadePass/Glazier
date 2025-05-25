namespace CascadePass.Glazier.UI.Tests
{
    [TestClass]
    public class ThemeDetectorTests
    {
        [TestMethod]
        public void Test_LightMode_Detection()
        {
            var testRegistryProvider = new MockRegistryProvider { ReturnValue = 1 };

            var themeDetector = new ThemeDetector(testRegistryProvider);
            Assert.IsTrue(themeDetector.IsInLightMode);
        }

        [TestMethod]
        public void Test_DarkMode_Detection()
        {
            var testRegistryProvider = new MockRegistryProvider { ReturnValue = 0 };

            var themeDetector = new ThemeDetector(testRegistryProvider);
            Assert.IsFalse(themeDetector.IsInLightMode);
        }
    }
}
