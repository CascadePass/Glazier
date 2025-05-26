using System.Collections.Generic;

namespace CascadePass.Glazier.UI
{
    public class GlazeMethodViewModel : ViewModel
    {
        #region Fields

        private string name;
        private string description;
        private string iconPath;
        private GlazeMethod method;
        private string methodDescription;

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

        #endregion

        public static IEnumerable<GlazeMethodViewModel> GetMethods()
        {
            return
            [
                new GlazeMethodViewModel
                {
                    Name = Resources.Prism,
                    Description = Resources.AlgorithmDescription_Prism,
                    IconPath = "/Images/Icons/ColorPalette.png",
                    Method = GlazeMethod.Prism_ColorReplacement,
                },

                new GlazeMethodViewModel
                {
                    Name = Resources.Onyx,
                    Description = Resources.AlgorithmDescription_Onyx,
                    IconPath = "/Images/Icons/ColorDialog.png",
                    Method = GlazeMethod.Onyx_MachineLearning,
                }
            ];
        }
    }
}
