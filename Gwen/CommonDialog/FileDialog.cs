using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Gwen.Control;

namespace Gwen.CommonDialog
{
    /// <summary>
    /// Base class for a file or directory dialog.
    /// </summary>
    public abstract class FileDialog : WindowControl
    {
        private Action<string>? m_Callback;

        private string m_CurrentFolder;
        private string m_CurrentFilter;

        private bool m_FoldersOnly;

        private bool m_OnClosing;

        private TreeControl m_Folders;
        private ListBox m_Items;
        private TextBox m_Path;
        private TextBox m_SelectedName;
        private ComboBox m_Filters;
        private Button m_Ok;
        private Button m_NewFolder;
        private VerticalSplitter m_NameFilterSplitter;
        private Label m_FileNameLabel;

        /// <summary>
        /// Initial folder for the dialog.
        /// </summary>
        public string InitialFolder { set { SetPath(value); } }

        /// <summary>
        /// Set initial folder and selected item.
        /// </summary>
        public string CurrentItem { set { SetPath(Path.GetDirectoryName(value) ?? throw new NullReferenceException()); SetCurrentItem(Path.GetFileName(value)); } }

        /// <summary>
        /// File filters. See <see cref="SetFilters(string, int)"/>.
        /// </summary>
        public string Filters { set { SetFilters(value); } }

        /// <summary>
        /// Text shown in the ok button.
        /// </summary>
        public string OkButtonText { get { return m_Ok.Text; } set { m_Ok.Text = value; } }

        /// <summary>
        /// Function that is called when dialog is closed. If ok is pressed, parameter is the selected file / directory.
        /// If cancel is pressed or window closed, parameter is null.
        /// </summary>
        public Action<string> Callback { get { return m_Callback; } set { m_Callback = value; } }

        /// <summary>
        /// Hide or show new folder button.
        /// </summary>
        public bool EnableNewFolder { get { return !m_NewFolder.IsHidden; } set { m_NewFolder.IsHidden = !value; } }

        /// <summary>
        /// Show only directories.
        /// </summary>
        protected bool FoldersOnly
        {
            get { return m_FoldersOnly; }
            set
            {
                m_FoldersOnly = value;
                m_Filters.IsHidden = value;
                m_FileNameLabel.Text = "Folder name:";
                if (value)
                    m_NameFilterSplitter.Zoom(0);
                else
                    m_NameFilterSplitter.UnZoom();
            }
        }

        /// <summary>
        /// Constructor for the base class. Implementing classes must call this.
        /// </summary>
        /// <param name="parent">Parent.</param>
        protected FileDialog(Base parent)
            : base(parent)
        {
        }

        protected override void OnCreated()
        {
            m_Folders = FindChildByName<TreeControl>("Folders");
            m_Items = FindChildByName<ListBox>("Items");
            m_Path = FindChildByName<TextBox>("Path");
            m_SelectedName = FindChildByName<TextBox>("SelectedName");
            m_Filters = FindChildByName<ComboBox>("Filters");
            m_Ok = FindChildByName<Button>("Ok");
            m_NewFolder = FindChildByName<Button>("NewFolder");
            m_NameFilterSplitter = FindChildByName<VerticalSplitter>("NameFilterSplitter");
            m_FileNameLabel = FindChildByName<Label>("FileNameLabel");

            UpdateFolders();

            m_OnClosing = false;

            m_CurrentFolder = Directory.GetCurrentDirectory();

            m_CurrentFilter = "*.*";
            m_Filters.AddItem("All files (*.*)", "All files (*.*)", "*.*");
        }

        /// <summary>
        /// Set current path.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <returns>True if the path change was successful. False otherwise.</returns>
        public bool SetPath(string path)
        {
            if (Directory.Exists(path))
            {
                m_CurrentFolder = path;
                m_Path.Text = m_CurrentFolder;
                UpdateItemList();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set filters.
        /// </summary>
        /// <param name="filterStr">Filter string. Format 'name|filter[|name|filter]...'</param>
        /// <param name="current">Set this index as a current filter.</param>
        public void SetFilters(string filterStr, int current = 0)
        {
            string[] filters = filterStr.Split('|');
            if ((filters.Length & 0x1) == 0x1)
                throw new Exception("Error in filter.");

            m_Filters.DeleteAll();

            for (int i = 0; i < filters.Length; i += 2)
            {
                m_Filters.AddItem(filters[i], filters[i], filters[i + 1]);
            }

            m_Filters.SelectedIndex = current;
        }

        /// <summary>
        /// Set current file or directory.
        /// </summary>
        /// <param name="item">File or directory. This doesn't need to exists.</param>
        protected void SetCurrentItem(string item)
        {
            m_SelectedName.Text = item;
        }

        /// <summary>
        /// Close the dialog and call the call back function.
        /// </summary>
        /// <param name="path">Parameter for the call back function.</param>
        protected void Close(string? path)
        {
            OnClosing(path, true);
        }

        /// <summary>
        /// Called when the user selects a file or directory.
        /// </summary>
        /// <param name="path">Full path of selected file or directory.</param>
        protected virtual void OnItemSelected(string path)
        {
            if ((Directory.Exists(path) && m_FoldersOnly) || (File.Exists(path) && !m_FoldersOnly))
            {
                SetCurrentItem(Path.GetFileName(path));
            }
        }

        /// <summary>
        /// Called to validate the file or directory name when the user enters it.
        /// </summary>
        /// <param name="path">Full path of the name.</param>
        /// <returns>Is the name valid.</returns>
        protected virtual bool IsSubmittedNameOk(string path)
        {
            if (Directory.Exists(path))
            {
                if (!m_FoldersOnly)
                {
                    SetPath(path);
                }
            }
            else if (File.Exists(path))
            {
                return true;
            }
            else
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called to validate the path when the user presses the ok button.
        /// </summary>
        /// <param name="path">Full path.</param>
        /// <returns>Is the path valid.</returns>
        protected virtual bool ValidateFileName(string path)
        {
            return true;
        }

        /// <summary>
        /// Called when the dialog is closing.
        /// </summary>
        /// <param name="path">Path for the call back function</param>
        /// <param name="doClose">True if the dialog needs to be closed.</param>
        protected virtual void OnClosing(string? path, bool doClose)
        {
            if (m_OnClosing)
                return;

            m_OnClosing = true;

            Close();

            if (m_Callback != null)
                m_Callback(path);
        }

        private void OnPathSubmitted(Control.Base sender, EventArgs args)
        {
            if (!SetPath(m_Path.Text))
            {
                m_Path.Text = m_CurrentFolder;
            }
        }

        private void OnUpClicked(Control.Base sender, ClickedEventArgs args)
        {
            string? newPath = Path.GetDirectoryName(m_CurrentFolder);
            if (newPath != null)
            {
                SetPath(newPath);
            }
        }

        private void OnNewFolderClicked(Control.Base sender, ClickedEventArgs args)
        {
            string path = m_Path.Text;
            if (Directory.Exists(path))
            {
                m_Path.Focus();
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(path);
                    SetPath(path);
                }
                catch (Exception ex) {
                    var box = new MessageBox(this, ex.Message, Title);
                    box.Show();
                }
            }
        }

        private void OnFolderSelected(Control.Base sender, EventArgs args)
        {
            TreeNode? node = sender as TreeNode;
            if (node != null && node.UserData != null)
            {
                SetPath(node.UserData as string);
            }
        }

        private void OnItemSelected(Control.Base sender, ItemSelectedEventArgs args)
        {
            string path = args.SelectedItem.UserData as string;
            if (path != null)
            {
                OnItemSelected(path);
            }
        }

        private void OnItemDoubleClicked(Control.Base sender, ItemSelectedEventArgs args)
        {
            string path = args.SelectedItem.UserData as string;
            if (path != null)
            {
                if (Directory.Exists(path))
                {
                    SetPath(path);
                }
                else
                {
                    OnOkClicked(null, null);
                }
            }
        }

        private void OnNameSubmitted(Control.Base sender, EventArgs args)
        {
            string path = Path.Combine(m_CurrentFolder, m_SelectedName.Text);
            if (IsSubmittedNameOk(path))
                OnOkClicked(null, null);
        }

        private void OnFilterSelected(Control.Base sender, ItemSelectedEventArgs args)
        {
            m_CurrentFilter = m_Filters.SelectedItem.UserData as string;
            UpdateItemList();
        }

        private void OnOkClicked(Control.Base? sender, ClickedEventArgs? args)
        {
            string path = Path.Combine(m_CurrentFolder, m_SelectedName.Text);
            if (ValidateFileName(path))
            {
                OnClosing(path, true);
            }
        }

        private void OnCancelClicked(Control.Base sender, ClickedEventArgs args)
        {
            OnClosing(null, true);
        }

        private void OnWindowClosed(Control.Base sender, EventArgs args)
        {
            OnClosing(null, false);
        }

        private void UpdateItemList()
        {
            m_Items.Clear();

            IOrderedEnumerable<DirectoryInfo> directories;
            IOrderedEnumerable<FileInfo>? files = null;
            try
            {
                directories = Directory.GetDirectories(m_CurrentFolder).Select(p => new DirectoryInfo(p)).OrderBy(di => di.Name);
                if (!m_FoldersOnly)
                    files = Directory.GetFiles(m_CurrentFolder, m_CurrentFilter).Select(f => new FileInfo(f)).OrderBy(fi => fi.Name);
            }
            catch (Exception ex)
            {
                var msgBox = new MessageBox(this, ex.Message, Title);
                msgBox.Show();
                return;
            }

            foreach (DirectoryInfo di in directories)
            {
                ListBoxRow row = m_Items.AddRow(di.Name, null, di.FullName);
                row.SetCellText(1, "<dir>");
                row.SetCellText(2, di.LastWriteTime.ToString(CultureInfo.InvariantCulture));
            }

            if (!m_FoldersOnly)
            {
                foreach (FileInfo fi in files)
                {
                    ListBoxRow row = m_Items.AddRow(fi.Name, null, fi.FullName);
                    row.SetCellText(1, fi.Length.ToString());
                    row.SetCellText(2, fi.Length.ToString());
                }
            }
        }

        private void UpdateFolders()
        {
            m_Folders.DeleteAllChildren();

            foreach (ISpecialFolder folder in Platform.GwenPlatform.GetSpecialFolders())
            {
                TreeNode category = m_Folders.FindNodeByName(folder.Category, false);
                if (category == null)
                    category = m_Folders.AddNode(folder.Category, folder.Category, null);

                category.AddNode(folder.Name, folder.Name, folder.Path);
            }

            m_Folders.ExpandAll();
        }

        private string FormatFileLength(long length)
        {
            if (length > 1024 * 1024 * 1024)
                return String.Format("{0:0.0} GB", (double)length / (1024 * 1024 * 1024));
            else if (length > 1024 * 1024)
                return String.Format("{0:0.0} MB", (double)length / (1024 * 1024));
            else if (length > 1024)
                return String.Format("{0:0.0} kB", (double)length / 1024);
            else
                return String.Format("{0} B", length);
        }

        private string FormatFileTime(DateTime dateTime)
        {
            return "";
            //return String.Format("{0} {1}", dateTime.ToShortDateString(), dateTime.ToLongTimeString());
        }

        private static readonly string Xml = @"<?xml version='1.0' encoding='UTF-8'?>
			<Window Size='400,300' StartPosition='CenterCanvas' Closed='OnWindowClosed'>
				<DockLayout Margin='2' >
					<DockLayout Dock='Top'>
						<Label Dock='Left' Margin='2' Alignment='CenterV,Left' Text='Path:' />
						<TextBox Name='Path' Margin='2' Dock='Fill' SubmitPressed='OnPathSubmitted' />
						<Button Name='NewFolder' Margin='2' Dock='Right' Padding='10,0,10,0' Text='New' Clicked='OnNewFolderClicked' />
						<Button Name='Up' Margin='2' Dock='Right' Padding='10,0,10,0' Text='Up' Clicked='OnUpClicked' />
					</DockLayout>
					<VerticalSplitter Dock='Fill' Value='0.3' SplitterSize='2'>
						<TreeControl Name='Folders' Margin='2' Selected='OnFolderSelected' />
						<ListBox Name='Items' Margin='2' ColumnCount='3' RowSelected='OnItemSelected' RowDoubleClicked='OnItemDoubleClicked' />
					</VerticalSplitter>
					<DockLayout Dock='Bottom'>
						<Button Name='Cancel' Margin='2' Dock='Right' Padding='10,0,10,0' Width='100' Text='Cancel' Clicked='OnCancelClicked' />
						<Button Name='Ok' Margin='2' Dock='Right' Padding='10,0,10,0' Width='100' Text='Ok' Clicked='OnOkClicked' />
					</DockLayout>
					<VerticalSplitter Name='NameFilterSplitter' Dock='Bottom' Value='0.7' SplitterSize='2'>
						<DockLayout>
							<Label Name='FileNameLabel' Dock='Left' Margin='2' Alignment='CenterV,Left' Text='File name:' />
							<TextBox Name='SelectedName' Dock='Fill' Margin='2' SubmitPressed='OnNameSubmitted'/>
						</DockLayout>
						<ComboBox Name='Filters' Margin='2' ItemSelected='OnFilterSelected'/>
					</VerticalSplitter>
				</DockLayout>
			</Window>
			";
    }
}