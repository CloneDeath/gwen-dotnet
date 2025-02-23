using Gwen.Net.Control;
using static Gwen.Net.Platform.GwenPlatform;

namespace Gwen.Net.CommonDialog
{
    /// <summary>
    /// Dialog for selecting an existing directory.
    /// </summary>
    public class FolderBrowserDialog : FileDialog
    {
        public FolderBrowserDialog(ControlBase parent)
            : base(parent)
        {
        }

        protected override void OnCreated()
        {
            base.OnCreated();

            FoldersOnly = true;
            Title = "Select Folder";
            OkButtonText = "Select";
        }

        protected override void OnItemSelected(string path)
        {
            if (DirectoryExists(path))
            {
                SetCurrentItem(GetFileName(path));
            }
        }

        protected override bool IsSubmittedNameOk(string path)
        {
            if (DirectoryExists(path))
            {
                SetPath(path);
                return true;
            }

            return false;
        }

        protected override bool ValidateFileName(string path)
        {
            return DirectoryExists(path);
        }
    }
}