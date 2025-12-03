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

        // ホットキーID
        private const int HOTKEY_ID_NEW_NOTE = 1;
        private const int HOTKEY_ID_TOGGLE_NOTES = 2;

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
                    HOTKEY_ID_NEW_NOTE,
                    (uint)(KeyModifier.Control | KeyModifier.Shift),
                    (uint)Keys.N
                );

                // Ctrl+Shift+H: すべての付箋を表示/非表示
                bool result2 = RegisterHotKey(
                    windowHandle,
                    HOTKEY_ID_TOGGLE_NOTES,
                    (uint)(KeyModifier.Control | KeyModifier.Shift),
                    (uint)Keys.H
                );

                if (result1 && result2)
                {
                    System.Diagnostics.Debug.WriteLine("ホットキー登録成功");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ホットキー登録失敗");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ホットキー登録エラー: {ex.Message}");
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
                UnregisterHotKey(windowHandle, HOTKEY_ID_NEW_NOTE);
                UnregisterHotKey(windowHandle, HOTKEY_ID_TOGGLE_NOTES);
                System.Diagnostics.Debug.WriteLine("ホットキー解除完了");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ホットキー解除エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// ホットキーが押されたときの処理
        /// </summary>
        public void ProcessHotkey(int hotkeyId)
        {
            switch (hotkeyId)
            {
                case HOTKEY_ID_NEW_NOTE:
                    System.Diagnostics.Debug.WriteLine("ホットキー: 新しい付箋を作成");
                    NewNoteRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case HOTKEY_ID_TOGGLE_NOTES:
                    System.Diagnostics.Debug.WriteLine("ホットキー: 付箋の表示/非表示切り替え");
                    ToggleNotesRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}