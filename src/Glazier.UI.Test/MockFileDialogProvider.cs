namespace CascadePass.Glazier.UI.Tests
{
    public class MockFileDialogProvider : IFileDialogProvider
    {
        public string SelectedFilePath { get; set; }

        public string BrowseToOpenImageFile()
        {
            return this.SelectedFilePath;
        }

        public string BrowseToSaveImageFile()
        {
            return this.SelectedFilePath;
        }
    }
}
