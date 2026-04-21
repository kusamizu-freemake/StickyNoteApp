using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// グローバルホットキー管理クラス
    /// アプリ全体で使用するホットキーの登録・解除・処理を管理する
    /// </summary>
    public class HotkeyManager
    {
        // Windows API（グローバルホットキー登録）
        [DllImport("user32.dll")]
        // 指定したキーの組み合わせをグローバルホットキーとしてWindowsに登録する
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        // 登録済みのグローバルホットキーを解除し、Windowsへ返却
        // 解除しない場合、他のアプリで同じキーが使用できなくなる場合がある
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// ホットキーで使う修飾キー（Ctrl・Alt・Shift・Winキー）の定数定義
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
        /// アプリ起動時に呼び出される
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
                    (uint)(KeyModifier.Control | KeyModifier.Shift), // Ctrl+Shift
                    (uint)Keys.N
                );

                // Ctrl+Shift+H: すべての付箋を表示/非表示
                bool result2 = RegisterHotKey(
                    windowHandle,
                    AppConstants.HotkeyConfig.HOTKEY_ID_TOGGLE_NOTES,
                    (uint)(KeyModifier.Control | KeyModifier.Shift), // Ctrl+Shift
                    (uint)Keys.H
                );

                // 両方の登録結果を確認してログに記録
                if (result1 && result2)
                {
                    System.Diagnostics.Debug.WriteLine(AppConstants.HotkeyMsg.MSG_REGISTER_SUCCESS);
                    return true;
                }
                else
                {
                    // 同じキーを別のアプリが先に登録している場合などに失敗
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
        /// アプリ終了時に必ず呼び出す必要がある
        /// 解除しないとキーが他アプリで使用できなくなる場合がある
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
        /// ホットキー押下時の処理振り分け
        /// WndProc から呼び出される
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