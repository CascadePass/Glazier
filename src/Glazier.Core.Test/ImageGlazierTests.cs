using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CascadePass.Glazier.Core.Test
{
    [TestClass]
    public class ImageGlazierTests
    {
        #region LoadImage

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void LoadImage_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
        {
            var glazier = new ImageGlazier();
            string invalidFilePath = $"{Guid.NewGuid()}_nonexistent.png";

            glazier.LoadImage(invalidFilePath);
        }

        [TestMethod]
        public void LoadImage_ShouldLoadImage_WhenFileExists()
        {
            var glazier = new ImageGlazier();
            string validFilePath = "test.png";

            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Save(validFilePath);
            }

            glazier.LoadImage(validFilePath);

            Assert.IsNotNull(glazier.ImageData);
            Assert.AreEqual(100, glazier.ImageData.Width);
            Assert.AreEqual(100, glazier.ImageData.Height);

            File.Delete(validFilePath);
        }

        #endregion

        #region SaveImage

        [TestMethod]
        public void SaveImage_ShouldSaveImageToFile()
        {
            var glazier = new ImageGlazier();
            string outputPath = "output.png";

            glazier.ImageData = new Image<Rgba32>(100, 100);

            glazier.SaveImage(outputPath);

            Assert.IsTrue(File.Exists(outputPath));

            File.Delete(outputPath);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SaveImage_ShouldThrowInvalidOperationException_WhenImageDataIsNull()
        {
            string outputPath = "output.png";
            var glazier = new ImageGlazier();

            glazier.SaveImage(outputPath);
        }

        #endregion

        [TestMethod]
        public void GetMostCommonColors_ShouldReturnCorrectColors()
        {
            var glazier = new ImageGlazier
            {
                ImageData = new Image<Rgba32>(2, 2)
            };

            glazier.ImageData[0, 0] = new Rgba32(255, 0, 0);
            glazier.ImageData[0, 1] = new Rgba32(255, 0, 0);
            glazier.ImageData[1, 0] = new Rgba32(0, 255, 0);
            glazier.ImageData[1, 1] = new Rgba32(0, 0, 255);

            var result = glazier.GetMostCommonColors(2);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey(new Rgba32(255, 0, 0)));
            Assert.IsTrue(result.ContainsKey(new Rgba32(0, 255, 0)));
        }

        [TestMethod]
        public void Glaze_ShouldReplaceMatchingColors()
        {
            var glazier = new ImageGlazier
            {
                ImageData = new Image<Rgba32>(2, 2)
            };

            glazier.ImageData[0, 0] = new Rgba32(255, 0, 0);
            glazier.ImageData[0, 1] = new Rgba32(255, 0, 0);
            glazier.ImageData[1, 0] = new Rgba32(0, 255, 0);
            glazier.ImageData[1, 1] = new Rgba32(0, 0, 255);

            var matchColor = new Rgba32(255, 0, 0);
            var replacementColor = new Rgba32(0, 0, 0, 0);

            glazier.Glaze(matchColor, replacementColor, 0);

            Assert.AreEqual(replacementColor, glazier.ImageData[0, 0]);
            Assert.AreEqual(replacementColor, glazier.ImageData[0, 1]);
            Assert.AreNotEqual(replacementColor, glazier.ImageData[1, 0]);
            Assert.AreNotEqual(replacementColor, glazier.ImageData[1, 1]);
        }

        [TestMethod]
        public void Dispose_ShouldReleaseImageData()
        {
            var glazier = new ImageGlazier();
            glazier.ImageData = new Image<Rgba32>(100, 100);

            glazier.Dispose();

            Assert.IsNull(glazier.ImageData);
        }
    }
}
