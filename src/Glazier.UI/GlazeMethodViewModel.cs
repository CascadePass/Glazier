using System.Collections.Generic;

namespace CascadePass.Glazier.UI
{
    public class GlazeMethodViewModel : ViewModel
    {
        public GlazeMethodViewModel() { }

        public string Name { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }
        public GlazeMethod Method { get; set; }
        public string MethodDescription { get; set; }

        public static IEnumerable<GlazeMethodViewModel> GetMethods()
        {
            return
            [
                new GlazeMethodViewModel
                {
                    Name = "Color and similarity",
                    Description = "Fast, good for simple backgrounds",
                    IconPath = "/Images/Icons/ColorPalette.png",
                    Method = GlazeMethod.ColorReplacement,
                    MethodDescription = "Replaces a color and similar hues, with transparent"
                },
                new GlazeMethodViewModel
                {
                    Name = "Onyx (Machine Learning)",
                    Description = "Slower, better for complex backgrounds",
                    IconPath = "/Images/Icons/ColorDialog.png",
                    Method = GlazeMethod.MachineLearning,
                    MethodDescription = "Uses a machine learning data set to identify the subject and remove everything else"
                }
            ];
        }
    }
}
