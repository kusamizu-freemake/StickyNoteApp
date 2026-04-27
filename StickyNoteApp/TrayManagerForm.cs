using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// タスクトレイ管理フォーム
    /// </summary>
    public partial class TrayManagerForm : Form
    {
        private static NotifyIcon TrayIcon; // タスクトレイアイコン
        private ContextMenuStrip TrayMenu; // トレイメニュー
        private ToolStripMenuItem ToggleAllNotesMenuItem; // すべての付箋表示/非表示メニュー

        // 全付箋を追跡するリスト
        private List<StickyNoteForm> allNotes = new List<StickyNoteForm>();
        private bool allNotesVisible = true;

        // ホットキーマネージャー
        private HotkeyManager HotkeyManager;

        // アプリ設定（フォント・フォントサイズ）
        private AppSettings _appSettings;

        // 常駐開始
        public TrayManagerForm()
        {
            InitializeComponent(); // フォームデザイナーで設定したUI要素の初期化

            // アプリ設定を読み込む
            _appSettings = AppSettings.Load();

            InitializeTray(); // トレイアイコン初期化

            // ホットキーマネージャーの初期化
            HotkeyManager = new HotkeyManager();
            HotkeyManager.NewNoteRequested += (s, e) => OnCreateNoteClicked(s, e);
            HotkeyManager.ToggleNotesRequested += (s, e) => OnShowHideAllStickyNotesClicked(s, e);

            RestoreNotes(); // 付箋を復元
        }

        /// <summary>
        /// タスクトレイの初期化
        /// </summary>
        private void InitializeTray()
        {
            // 右クリック時に表示されるメニュー
            TrayMenu = new ContextMenuStrip();

            // メニュー項目にショートカットキーを表示
            var NewNoteItem = new ToolStripMenuItem(AppConstants.TrayManagerMsg.MSG_MENU_NEW_NOTE);
            NewNoteItem.ShortcutKeyDisplayString = AppConstants.TrayManagerMsg.MSG_MENU_SHORTCUT_NEW_NOTE;
            NewNoteItem.Click += OnCreateNoteClicked;
            TrayMenu.Items.Add(NewNoteItem);

            ToggleAllNotesMenuItem = new ToolStripMenuItem(AppConstants.TrayManagerMsg.MSG_MENU_HIDE_ALL_NOTES);
            ToggleAllNotesMenuItem.ShortcutKeyDisplayString = AppConstants.TrayManagerMsg.MSG_MENU_SHORTCUT_TOGGLE;
            ToggleAllNotesMenuItem.Click += OnShowHideAllStickyNotesClicked;
            TrayMenu.Items.Add(ToggleAllNotesMenuItem);

            // DB整合性チェック（デバッグ用。完成間際で削除予定）
            TrayMenu.Items.Add(new ToolStripSeparator()); // 区切り線
            TrayMenu.Items.Add(AppConstants.TrayManagerMsg.MSG_MENU_DB_INTEGRITY_CHECK, null, OnDatabaseIntegrityCheckClicked);

            // 設定（未実装）
            TrayMenu.Items.Add(AppConstants.TrayManagerMsg.MSG_MENU_SETTINGS, null, OnSettingClicked);
            TrayMenu.Items.Add(new ToolStripSeparator()); // 区切り線
            TrayMenu.Items.Add(AppConstants.TrayManagerMsg.MSG_MENU_EXIT, null, OnExitClicked);

            // タスクトレイアイコンの設定
            TrayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                ContextMenuStrip = TrayMenu,
                Text = AppConstants.TrayManagerMsg.MSG_TRAY_TOOLTIP_TEXT
            };

            TrayIcon.MouseClick += TrayIcon_MouseClick;
        }

        /// <summary>
        /// トレイアイコン左クリック時の処理（デバッグ用）
        /// </summary>
        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                TrayIcon.ShowBalloonTip(
                    AppConstants.TrayManagerConfig.TRAY_BALLOON_TIP_DURATION_SHORT,
                    AppConstants.TrayManagerMsg.MSG_BALLOON_STARTUP_TITLE,
                    AppConstants.TrayManagerMsg.MSG_BALLOON_CLICK_TEXT,
                    ToolTipIcon.Info);
            }
        }

        /// <summary>
        /// 保存された付箋を復元
        /// </summary>
        private void RestoreNotes()
        {
            int restoredCount = 0;         // 復元した付箋の数をカウント
            int reminderRestoredCount = 0; // リマインダー復元数

            try
            {
                // グローバル復元フラグを立てる（起動時は既にtrueだが念のため）
                Common.BeginRestore();

                // データベースから全データを一括取得（接続はすぐに閉じられる）
                List<StickyNoteData> noteDataList = Database.LoadAll();

                System.Diagnostics.Debug.WriteLine(
                    string.Format(AppConstants.TrayManagerMsg.MSG_RESTORE_START, noteDataList.Count));

                // 取得したデータを元に付箋を復元
                foreach (var noteData in noteDataList)
                {
                    restoredCount++;

                    StickyNoteForm note = new StickyNoteForm();

                    // データベースから値を復元
                    note.NoteId = noteData.Id;
                    note.CreatedAt = noteData.CreatedAt;

                    // 色を先に復元
                    Color bgColor = Color.FromArgb(noteData.BgR, noteData.BgG, noteData.BgB);
                    note.BackColor = bgColor;
                    note.txtNote.BackColor = bgColor;

                    // 位置とサイズを復元
                    note.Location = new Point(noteData.PosX, noteData.PosY);
                    note.Size = new Size(noteData.Width, noteData.Height);

                    // TopMostを復元
                    bool topMost = noteData.TopMostFlag == AppConstants.TrayManagerConfig.TOPMOST_FLAG_ENABLED;
                    note.SetTopMost(topMost);

                    // テキストを最後に復元
                    note.SetText(noteData.Content);

                    // 画像を復元（3つのパスを渡す）
                    if (!string.IsNullOrEmpty(noteData.ImagePath) ||
                        !string.IsNullOrEmpty(noteData.OriginalImagePath) ||
                        !string.IsNullOrEmpty(noteData.ResizedImagePath))
                    {
                        note.LoadCapturedImage(
                            noteData.ImagePath,
                            noteData.OriginalImagePath,
                            noteData.ResizedImagePath,
                            noteData.ImageDisplayHeight
                        );
                    }

                    // リマインダー復元
                    if (noteData.ReminderActive == 1 && !string.IsNullOrEmpty(noteData.ReminderTime))
                    {
                        DateTime reminderTime;
                        if (DateTime.TryParse(noteData.ReminderTime, out reminderTime))
                        {
                            if (reminderTime > DateTime.Now)
                            {
                                note.RestoreReminder(reminderTime);
                                reminderRestoredCount++;
                                System.Diagnostics.Debug.WriteLine(
                                    string.Format(AppConstants.TrayManagerMsg.MSG_REMINDER_RESTORED,
                                        noteData.Id, reminderTime));
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    string.Format(AppConstants.TrayManagerMsg.MSG_REMINDER_EXPIRED,
                                        noteData.Id, reminderTime));
                            }
                        }
                    }
                    // 個別のEndRestore()は不要

                    allNotes.Add(note);

                    // 付箋が閉じられたらリストから削除
                    note.FormClosed += (s, e) =>
                    {
                        allNotes.Remove(note);
                    };

                    // フォント設定を適用してから表示
                    ApplySettingsToNote(note, _appSettings);

                    // 付箋を表示
                    note.Show();
                }

                System.Diagnostics.Debug.WriteLine(
                    string.Format(AppConstants.TrayManagerMsg.MSG_RESTORE_COMPLETE,
                        restoredCount, reminderRestoredCount));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    string.Format(AppConstants.TrayManagerMsg.MSG_RESTORE_ERROR, ex.Message));
                MessageBox.Show(
                    string.Format(AppConstants.TrayManagerMsg.MSG_RESTORE_ERROR, ex.Message),
                    AppConstants.SharedTitle.ERROR,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // 重要：すべての復元処理が完了したらグローバルフラグを下ろす
                Common.EndRestore();
            }
        }

        /// <summary>
        /// 新しい付箋を作成
        /// </summary>
        private void OnCreateNoteClicked(object sender, EventArgs e)
        {
            StickyNoteForm note = new StickyNoteForm();
            note.Location = new Point(
                Cursor.Position.X - AppConstants.TrayManagerConfig.CURSOR_OFFSET,
                Cursor.Position.Y - AppConstants.TrayManagerConfig.CURSOR_OFFSET
            ); // カーソル位置に表示

            // 付箋をリストに追加
            allNotes.Add(note);

            // 付箋が閉じられたらリストから削除
            note.FormClosed += (s, ev) =>
            {
                allNotes.Remove(note);
            };

            // フォント設定を適用してから表示
            ApplySettingsToNote(note, _appSettings);

            note.Show();
        }
        private void OnShowHideAllStickyNotesClicked(object sender, EventArgs e)
        {
            // 閉じられた付箋をリストから削除
            allNotes.RemoveAll(n => n.IsDisposed);

            if (allNotesVisible)
            {
                // 非表示にする
                foreach (var note in allNotes)
                {
                    note.Hide();
                }
                allNotesVisible = false;
                ToggleAllNotesMenuItem.Text = AppConstants.TrayManagerMsg.MSG_MENU_SHOW_ALL_NOTES;
            }
            else
            {
                // 表示する
                foreach (var note in allNotes)
                {
                    note.Show();
                }
                allNotesVisible = true;
                ToggleAllNotesMenuItem.Text = AppConstants.TrayManagerMsg.MSG_MENU_HIDE_ALL_NOTES;
            }
        }

        /// <summary>
        /// データベース整合性チェック
        /// </summary>
        private void OnDatabaseIntegrityCheckClicked(object sender, EventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    AppConstants.TrayManagerMsg.MSG_INTEGRITY_CHECK_CONFIRM,
                    AppConstants.TrayManagerTitle.TITLE_INTEGRITY_CHECK,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    // 整合性チェック中は保存を停止
                    bool wasRestoring = Common.IsRestoring;
                    Common.BeginRestore();

                    try
                    {
                        // 整合性チェックと修正を実行
                        DatabaseIntegrityChecker.CheckAndRepair();

                        // 詳細レポートを表示するか確認
                        var reportResult = MessageBox.Show(
                            AppConstants.TrayManagerMsg.MSG_INTEGRITY_CHECK_DONE_CONFIRM,
                            AppConstants.SharedTitle.COMPLETE,
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information
                        );

                        if (reportResult == DialogResult.Yes)
                        {
                            DatabaseIntegrityChecker.GenerateReport();
                        }
                    }
                    finally
                    {
                        if (!wasRestoring)
                        {
                            // 保存を再開
                            Common.EndRestore();
                        }
                        System.Diagnostics.Debug.WriteLine(
                            AppConstants.TrayManagerMsg.MSG_INTEGRITY_CHECK_RESUME);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(AppConstants.TrayManagerMsg.MSG_INTEGRITY_CHECK_ERROR, ex.Message),
                    AppConstants.SharedTitle.ERROR,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// 設定（未実装）
        /// </summary>
        /// <summary>
        /// 設定画面を開く
        /// </summary>
        private void OnSettingClicked(object sender, EventArgs e)
        {
            using (var form = new SettingsForm(_appSettings))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _appSettings = form.GetSettings();
                    _appSettings.Save();

                    // 開いている全付箋に即時反映
                    ApplySettingsToAllNotes(_appSettings);

                    System.Diagnostics.Debug.WriteLine(AppConstants.SettingsMsg.MSG_APPLY_ALL);
                }
            }
        }

        /// <summary>
        /// 1つの付箋にフォント設定を適用する
        /// </summary>
        private void ApplySettingsToNote(StickyNoteForm note, AppSettings settings)
        {
            if (note == null || note.IsDisposed) return;

            try
            {
                note.txtNote.Font = new System.Drawing.Font(
                    settings.FontFamily,
                    settings.FontSize
                );
            }
            catch
            {
                // 無効なフォント名の場合はデフォルトにフォールバック
                note.txtNote.Font = new System.Drawing.Font(
                    AppConstants.SettingsConfig.DEFAULT_FONT_FAMILY,
                    AppConstants.SettingsConfig.DEFAULT_FONT_SIZE
                );
            }
        }

        /// <summary>
        /// 開いている全付箋にフォント設定を適用する（設定変更時に呼ぶ）
        /// </summary>
        private void ApplySettingsToAllNotes(AppSettings settings)
        {
            allNotes.RemoveAll(n => n.IsDisposed);
            foreach (var note in allNotes)
                ApplySettingsToNote(note, settings);
        }

        /// <summary>
        /// アプリケーション終了
        /// </summary>
        private void OnExitClicked(object sender, EventArgs e)
        {
            TrayIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// 起動時にフォームを表示しない（タスクトレイのみ）
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // ホットキーを登録
            if (HotkeyManager.RegisterHotkeys(this.Handle))
            {
                TrayIcon.ShowBalloonTip(
                    AppConstants.TrayManagerConfig.TRAY_BALLOON_TIP_DURATION,
                    AppConstants.TrayManagerMsg.MSG_BALLOON_STARTUP_TITLE,
                    AppConstants.TrayManagerMsg.MSG_BALLOON_STARTUP_TEXT,
                    ToolTipIcon.Info);
            }

            this.Hide(); // フォームを非表示にする
        }

        /// <summary>
        /// Windowsメッセージを処理（ホットキー用）
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                HotkeyManager.ProcessHotkey(hotkeyId);
            }
            // 自分で処理しなかったメッセージも含め、必ず基底クラスに渡す。
            // これを省略すると、ウィンドウの描画・移動・閉じるボタンなど Windows が本来行う処理がすべて止まってしまう。
            base.WndProc(ref m);
        }

        /// <summary>
        /// フォームクローズ時の処理
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 基底クラスの終了処理を先に実行する（省略すると正常に閉じられないことがある）。
            base.OnFormClosing(e);

            // ホットキーを解除
            // 登録したままアプリを終了すると、そのキーが他のアプリでも使えなくなる場合がある。
            if (HotkeyManager != null)
            {
                HotkeyManager.UnregisterHotkeys();
            }
        }
    }
}