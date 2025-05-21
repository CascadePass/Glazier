using Microsoft.Win32;

namespace CascadePass.Glazier.UI
{
    public interface IFileDialogProvider
    {
        string BrowseToOpenImageFile();
        string BrowseToSaveImageFile();
    }

    public class FileDialogProvider : IFileDialogProvider
    {
        public string BrowseToOpenImageFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select an Image File",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*",
                Multiselect = false
            };

            var result = openFileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                return openFileDialog.FileName;
            }

            return null;
        }

        public string BrowseToSaveImageFile()
        {
            var saveFileDialog = new SaveFileDialog()
            {
                Title = "Save Image File",
                Filter = "Image Files|*.png|Icon Files|*.ico|All Files|*.*",
                AddExtension = true,
                DefaultExt = "*.png"
            };

            var result = saveFileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                return saveFileDialog.FileName;
            }

            return null;
        }
    }
}
