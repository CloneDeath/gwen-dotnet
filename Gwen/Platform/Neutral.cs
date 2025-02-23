using System;
using System.Threading;
using TextCopy;

namespace Gwen.Platform
{
    /// <summary>
    /// Platform-agnostic utility functions.
    /// </summary>
    public static class Neutral
    {
        private static DateTime m_FirstTime = DateTime.Now;

        /// <summary>
        /// Changes the mouse cursor.
        /// </summary>
        /// <param name="cursor">Cursor type.</param>
        public static void SetCursor(Cursor cursor)
        {
            Cursor.Current = cursor;
        }


        /// <summary>
        /// Gets text from clipboard.
        /// </summary>
        /// <returns>Clipboard text.</returns>
        public static string? GetClipboardText()
        {
            return ClipboardService.GetText();
        }

        /// <summary>
        /// Sets the clipboard text.
        /// </summary>
        /// <param name="text">Text to set.</param>
        /// <returns>True if succeeded.</returns>
        public static void SetClipboardText(string text)
        {
            ClipboardService.SetText(text);
        }

        /// <summary>
        /// Gets elapsed time since this class was initalized.
        /// </summary>
        /// <returns>Time interval in seconds.</returns>
        public static float GetTimeInSeconds()
        {
            return (float)(DateTime.Now - m_FirstTime).TotalSeconds;
        }

        /// <summary>
        /// Displays an open file dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="startPath">Initial path.</param>
        /// <param name="extension">File extension filter.</param>
        /// <param name="callback">Callback that is executed after the dialog completes.</param>
        /// <returns>True if succeeded.</returns>
        public static bool FileOpen(string title, string startPath, string extension, Action<string> callback)
        {
            var dialog = new OpenFileDialog
                             {
                                 Title = title,
                                 InitialDirectory = startPath,
                                 DefaultExt = @"*.*",
                                 Filter = extension,
                                 CheckPathExists = true,
                                 Multiselect = false
                             };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (callback != null)
                {
                    callback(dialog.FileName);
                }
            }
            else
            {
                if (callback != null)
                {
                    callback(String.Empty);
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Displays a save file dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="startPath">Initial path.</param>
        /// <param name="extension">File extension filter.</param>
        /// <param name="callback">Callback that is executed after the dialog completes.</param>
        /// <returns>True if succeeded.</returns>
        public static bool FileSave(string title, string startPath, string extension, Action<string> callback)
        {
            var dialog = new SaveFileDialog
            {
                Title = title,
                InitialDirectory = startPath,
                DefaultExt = @"*.*",
                Filter = extension,
                CheckPathExists = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (callback != null)
                {
                    callback(dialog.FileName);
                }
            }
            else
            {
                if (callback != null)
                {
                    callback(String.Empty);
                }
                return false;
            }

            return true;
        }
    }
}
