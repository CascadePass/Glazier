﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CascadePass.Glazier.UI.Tests
{
    [TestClass]
    public class GlazeMethodConverterTests
    {
        [TestMethod]
        public void Convert_ShouldReturnTrue_WhenMethodsMatch()
        {
            var converter = new GlazeMethodConverter();
            var selectedMethod = GlazeMethod.Prism_ColorReplacement;
            var radioMethod = GlazeMethod.Prism_ColorReplacement;

            var result = converter.Convert(selectedMethod, typeof(bool), radioMethod, CultureInfo.InvariantCulture);

            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void Convert_ShouldReturnFalse_WhenMethodsDoNotMatch()
        {
            var converter = new GlazeMethodConverter();
            var selectedMethod = GlazeMethod.Onyx_MachineLearning;
            var radioMethod = GlazeMethod.Prism_ColorReplacement;

            var result = converter.Convert(selectedMethod, typeof(bool), radioMethod, CultureInfo.InvariantCulture);

            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void ConvertBack_ShouldReturnMethod_WhenCheckedTrue()
        {
            var converter = new GlazeMethodConverter();
            var radioMethod = GlazeMethod.Onyx_MachineLearning;

            var result = converter.ConvertBack(true, typeof(GlazeMethod), radioMethod, CultureInfo.InvariantCulture);

            Assert.AreEqual(radioMethod, result);
        }

        [TestMethod]
        public void ConvertBack_ShouldReturnBindingDoNothing_WhenNotChecked()
        {
            var converter = new GlazeMethodConverter();

            var result = converter.ConvertBack(false, typeof(GlazeMethod), GlazeMethod.Prism_ColorReplacement, CultureInfo.InvariantCulture);

            Assert.AreEqual(Binding.DoNothing, result);
        }
    }
}
