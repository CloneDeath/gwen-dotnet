using System;
using System.IO;
using Gwen.Control;

namespace Gwen.CommonDialog;

/// <summary>
/// Dialog for selecting a file name for saving or creating.
/// </summary>
public class SaveFileDialog : FileDialog
{
    public SaveFileDialog(Control.Base parent)
        : base(parent)
    {
    }

    protected override void OnCreated()
    {
        base.OnCreated();

        Title = "Save File";
        OkButtonText = "Save";
    }

    protected override void OnItemSelected(string path)
    {
        if (File.Exists(path))
        {
            SetCurrentItem(Path.GetFileName(path));
        }
    }

    protected override bool IsSubmittedNameOk(string path)
    {
        if (Directory.Exists(path))
        {
            SetPath(path);
        }
        else if (File.Exists(path))
        {
            return true;
        }

        return false;
    }

    protected override bool ValidateFileName(string path)
    {
        if (Directory.Exists(path))
            return false;

        if (File.Exists(path))
        {
            // TODO make this a MessageBoxButtons.YesNo control...
            MessageBox win = new MessageBox(this,
                $"File '{Path.GetFileName(path)}' already exists. Do you want to replace it?", Title);
            throw new NotImplementedException();
            // win.Dismissed += OnMessageBoxDismissed;
            win.UserData = path;
            return false;
        }

        return true;
    }

    // private void OnMessageBoxDismissed(Control.Base sender, MessageBoxResultEventArgs args)
    // {
    //     if (args.Result == MessageBoxResult.Yes)
    //         Close(sender.UserData as string);
    // }
}