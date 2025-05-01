using System;
using System.Windows;

namespace CascadePass.Glazier.UI.Tests
{
    [TestClass]
    public class IsNullOrWhitespaceConverterTests
    {
        [TestMethod]
        public void Space_Returns_True()
        {
            var converter = new IsNullOrWhitespaceConverter();
            var result = converter.Convert(" ", null, null, null);
            bool testResult = (bool)result;

            Assert.IsTrue(testResult);
        }

        [TestMethod]
        public void Tab_Returns_True()
        {
            var converter = new IsNullOrWhitespaceConverter();
            var result = converter.Convert("\t", null, null, null);
            bool testResult = (bool)result;

            Assert.IsTrue(testResult);
        }

        [TestMethod]
        public void Newline_Returns_True()
        {
            var converter = new IsNullOrWhitespaceConverter();
            var result = converter.Convert(Environment.NewLine, null, null, null);
            bool testResult = (bool)result;

            Assert.IsTrue(testResult);
        }

        [TestMethod]
        public void EmptyString_Returns_True()
        {
            var converter = new IsNullOrWhitespaceConverter();
            var result = converter.Convert(string.Empty, null, null, null);
            bool testResult = (bool)result;

            Assert.IsTrue(testResult);
        }

        [TestMethod]
        public void Null_Returns_True()
        {
            var converter = new IsNullOrWhitespaceConverter();
            var result = converter.Convert(null, null, null, null);
            bool testResult = (bool)result;

            Assert.IsTrue(testResult);
        }

        [TestMethod]
        public void Letter_Returns_False()
        {
            var converter = new IsNullOrWhitespaceConverter();
            var result = converter.Convert("A", null, null, null);
            bool testResult = (bool)result;

            Assert.IsFalse(testResult);
        }

        [TestMethod]
        public void Guid_Returns_False()
        {
            var converter = new IsNullOrWhitespaceConverter();
            var result = converter.Convert(Guid.NewGuid().ToString(), null, null, null);
            bool testResult = (bool)result;

            Assert.IsFalse(testResult);
        }

        [TestMethod]
        public void ConvertBack_Returns_UnsetValue()
        {
            var converter = new IsNullOrWhitespaceConverter();
            var result = converter.ConvertBack(Guid.NewGuid().ToString(), null, null, null);

            Assert.AreEqual(DependencyProperty.UnsetValue, result);
        }
    }
}
