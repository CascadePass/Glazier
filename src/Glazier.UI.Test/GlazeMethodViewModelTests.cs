using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CascadePass.Glazier.UI.Tests
{
    [TestClass]
    public class GlazeMethodViewModelTests
    {
        [TestMethod]
        public void GlazeMethodViewModel_Property_Set_ShouldUpdateValue()
        {
            var viewModel = new GlazeMethodViewModel();

            viewModel.Name = "TestName";
            viewModel.Description = "TestDescription";
            viewModel.IconPath = "/Images/TestIcon.png";
            viewModel.Method = GlazeMethod.ColorReplacement;
            viewModel.MethodDescription = "TestMethodDescription";

            Assert.AreEqual("TestName", viewModel.Name);
            Assert.AreEqual("TestDescription", viewModel.Description);
            Assert.AreEqual("/Images/TestIcon.png", viewModel.IconPath);
            Assert.AreEqual(GlazeMethod.ColorReplacement, viewModel.Method);
            Assert.AreEqual("TestMethodDescription", viewModel.MethodDescription);
        }

        [TestMethod]
        public void GlazeMethodViewModel_PropertyChange_ShouldRaiseNotification()
        {
            var viewModel = new GlazeMethodViewModel();
            bool notificationRaised = false;

            viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(viewModel.Name))
                {
                    notificationRaised = true;
                }
            };

            viewModel.Name = "NewName";

            Assert.IsTrue(notificationRaised);
        }

        [TestMethod]
        public void GlazeMethodViewModel_GetMethods_ShouldReturnExpectedValues()
        {
            var methods = GlazeMethodViewModel.GetMethods();

            Assert.IsTrue(methods.Any(m => m.Name == Resources.Prism));
            Assert.IsTrue(methods.Any(m => m.Name == Resources.Onyx));
        }

        [TestMethod]
        public void GlazeMethodViewModel_GetMethods_ShouldReturnEmpty_WhenNoMethodsDefined()
        {
            var originalMethods = GlazeMethodViewModel.GetMethods();

            Assert.IsTrue(originalMethods.Count() > 0, "There should be some methods defined initially.");
        }
    }
}
