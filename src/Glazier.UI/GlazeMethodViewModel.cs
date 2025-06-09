using System.Collections.Generic;

namespace CascadePass.Glazier.UI
{
    public class GlazeMethodViewModel : ViewModel
    {
        #region Fields

        private bool isSelected;
        private bool isEnabled;
        private string name;
        private string description;
        private string iconPath;
        private string methodDescription;
        private string currentStatus;
        private string modelPath;
        private GlazeMethod method;

        #endregion

        public GlazeMethodViewModel() { }

        #region Properties

        public string Name
        {
            get => this.name;
            set => this.SetPropertyValue(ref this.name, value, nameof(this.Name));
        }

        public string Description
        {
            get => this.description;
            set => this.SetPropertyValue(ref this.description, value, nameof(this.Description));
        }

        public string IconPath
        {
            get => this.iconPath;
            set => this.SetPropertyValue(ref this.iconPath, value, nameof(this.IconPath));
        }

        public GlazeMethod Method
        {
            get => this.method;
            set => this.SetPropertyValue(ref this.method, value, nameof(this.Method));
        }

        public string MethodDescription
        {
            get => this.methodDescription;
            set => this.SetPropertyValue(ref this.methodDescription, value, nameof(this.MethodDescription));
        }

        public bool IsSelected
        {
            get => this.isSelected;
            set => this.SetPropertyValue(ref this.isSelected, value, nameof(this.IsSelected));
        }

        public bool IsEnabled
        {
            get => this.isEnabled;
            set => this.SetPropertyValue(ref this.isEnabled, value, nameof(this.IsEnabled));
        }

        public string CurrentStatus
        {
            get => this.currentStatus;
            set => this.SetPropertyValue(ref this.currentStatus, value, nameof(this.CurrentStatus));
        }

        public string ModelPath
        {
            get => this.modelPath;
            set => this.SetPropertyValue(ref this.modelPath, value, nameof(this.ModelPath));
        }

        #endregion

        /// <summary>
        /// Gets a collection of available <see cref="GlazeMethodViewModel"/>s with info
        /// to show the user about the available methods.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="GlazeMethodViewModel"/>s.</returns>
        public static IEnumerable<GlazeMethodViewModel> GetMethods()
        {
            return
            [
                new GlazeMethodViewModel
                {
                    Name = Resources.Prism,
                    Description = Resources.AlgorithmDescription_Prism,
                    MethodDescription = Resources.AlgorithmMethod_Prism,
                    IconPath = "/Images/ColorPaintbrush.png",
                    Method = GlazeMethod.Prism_ColorReplacement,
                    IsEnabled = true,
                },

                new GlazeMethodViewModel
                {
                    Name = Resources.Onyx,
                    Description = Resources.AlgorithmDescription_Onyx,
                    MethodDescription = Resources.AlgorithmMethod_Onyx,
                    IconPath = "/Images/Onyx.3.png",
                    Method = GlazeMethod.Onyx_MachineLearning,
                    IsEnabled = false,
                }
            ];
        }
    }
}
