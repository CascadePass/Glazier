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

        #endregion
    }
}
