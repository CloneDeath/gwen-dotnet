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
    }
}
