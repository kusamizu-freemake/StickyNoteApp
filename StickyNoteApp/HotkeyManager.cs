using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// グローバルホットキー管理クラス
    /// </summary>
    public class HotkeyManager
    {
        // Windows API
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 修飾キー
        public enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        // ウィンドウハンドル
        private IntPtr windowHandle;

        // イベント
        public event EventHandler NewNoteRequested;
        public event EventHandler ToggleNotesRequested;

        /// <summary>
        /// ホットキーを登録
        /// </summary>
        public bool RegisterHotkeys(IntPtr handle)
        {
            windowHandle = handle;

            try
            {
                // Ctrl+Shift+N: 新しい付箋を作成
                bool result1 = RegisterHotKey(
                    windowHandle,
                    AppConstants.HotkeyConfig.HOTKEY_ID_NEW_NOTE,
                    (uint)(KeyModifier.Control | KeyModifier.Shift),
                    (uint)Keys.N
                );

                // Ctrl+Shift+H: すべての付箋を表示/非表示
                bool result2 = RegisterHotKey(
                    windowHandle,
                    AppConstants.HotkeyConfig.HOTKEY_ID_TOGGLE_NOTES,
                    (uint)(KeyModifier.Control | KeyModifier.Shift),
                    (uint)Keys.H
                );

                if (result1 && result2)
                {
                    System.Diagnostics.Debug.WriteLine(AppConstants.HotkeyMsg.MSG_REGISTER_SUCCESS);
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(AppConstants.HotkeyMsg.MSG_REGISTER_FAIL);
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format(AppConstants.HotkeyMsg.MSG_REGISTER_ERROR, ex.Message));
                return false;
            }
        }

        /// <summary>
        /// ホットキーを解除
        /// </summary>
        public void UnregisterHotkeys()
        {
            try
            {
                UnregisterHotKey(windowHandle, AppConstants.HotkeyConfig.HOTKEY_ID_NEW_NOTE);
                UnregisterHotKey(windowHandle, AppConstants.HotkeyConfig.HOTKEY_ID_TOGGLE_NOTES);
                System.Diagnostics.Debug.WriteLine(AppConstants.HotkeyMsg.MSG_UNREGISTER_DONE);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format(AppConstants.HotkeyMsg.MSG_UNREGISTER_ERROR, ex.Message));
            }
        }

        /// <summary>
        /// ホットキーが押されたときの処理
        /// </summary>
        public void ProcessHotkey(int hotkeyId)
        {
            switch (hotkeyId)
            {
                case AppConstants.HotkeyConfig.HOTKEY_ID_NEW_NOTE:
                    System.Diagnostics.Debug.WriteLine(AppConstants.HotkeyMsg.MSG_HOTKEY_NEW_NOTE);
                    NewNoteRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case AppConstants.HotkeyConfig.HOTKEY_ID_TOGGLE_NOTES:
                    System.Diagnostics.Debug.WriteLine(AppConstants.HotkeyMsg.MSG_HOTKEY_TOGGLE);
                    ToggleNotesRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}