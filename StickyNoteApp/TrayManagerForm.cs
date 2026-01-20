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
        // 定数定義
        private const int CURSOR_OFFSET = 50; // カーソル位置からの付箋作成オフセット
        private const int TOPMOST_FLAG_ENABLED = 1; // TopMostフラグが有効な場合の値
        private const int TRAY_BALLOON_TIP_DURATION = 2000; // トレイアイコンのバルーンチップ表示時間(ミリ秒)
        private const int TRAY_BALLOON_TIP_DURATION_SHORT = 1000; // トレイアイコンのバルーンチップ表示時間(短)(ミリ秒)


        private static NotifyIcon TrayIcon; // タスクトレイアイコン
        private ContextMenuStrip TrayMenu; // トレイメニュー
        private ToolStripMenuItem ToggleAllNotesMenuItem; // すべての付箋表示/非表示メニュー

        // 全付箋を追跡するリスト
        private List<StickyNoteForm> allNotes = new List<StickyNoteForm>();
        private bool allNotesVisible = true;

        // ホットキーマネージャー
        private HotkeyManager HotkeyManager;

        // 常駐開始
        public TrayManagerForm()
        {
            InitializeComponent(); // フォームデザイナーで設定したUI要素の初期化

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
            var NewNoteItem = new ToolStripMenuItem("新しい付箋を作成");
            NewNoteItem.ShortcutKeyDisplayString = "Ctrl+Shift+N";
            NewNoteItem.Click += OnCreateNoteClicked;
            TrayMenu.Items.Add(NewNoteItem);

            ToggleAllNotesMenuItem = new ToolStripMenuItem("すべての付箋を非表示");
            ToggleAllNotesMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+H";
            ToggleAllNotesMenuItem.Click += OnShowHideAllStickyNotesClicked;
            TrayMenu.Items.Add(ToggleAllNotesMenuItem);

            // DB整合性チェック（デバッグ用。完成間際で削除予定）
            TrayMenu.Items.Add(new ToolStripSeparator()); // 区切り線
            TrayMenu.Items.Add("データベース整合性チェック", null, OnDatabaseIntegrityCheckClicked);


            // 設定（未実装）
            TrayMenu.Items.Add("設定", null, OnSettingClicked);
            TrayMenu.Items.Add(new ToolStripSeparator()); // 区切り線
            TrayMenu.Items.Add("アプリを終了", null, OnExitClicked);

            // タスクトレイアイコンの設定
            TrayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                ContextMenuStrip = TrayMenu,
                Text = "付箋アプリ\nCtrl+Shift+N: 新規付箋\nCtrl+Shift+H: 表示/非表示"
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
                TrayIcon.ShowBalloonTip(TRAY_BALLOON_TIP_DURATION_SHORT, "付箋アプリ", "タスクトレイで動作中", ToolTipIcon.Info);
            }
        }

        /// <summary>
        /// 保存された付箋を復元
        /// </summary>
        private void RestoreNotes()
        {
            int restoredCount = 0; // 復元した付箋の数をカウント
            int reminderRestoredCount = 0; // リマインダー復元数

            try
            {
                // グローバル復元フラグを立てる（起動時は既にtrueだが念のため）
                Common.BeginRestore();

                // データベースから全データを一括取得（接続はすぐに閉じられる）
                List<StickyNoteData> noteDataList = Database.LoadAll();

                System.Diagnostics.Debug.WriteLine($"復元開始: {noteDataList.Count}件の付箋を処理します");

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
                    bool topMost = noteData.TopMostFlag == TOPMOST_FLAG_ENABLED;
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
                                    System.Diagnostics.Debug.WriteLine($"リマインダー復元: {noteData.Id} - {reminderTime}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"リマインダー期限切れ: {noteData.Id} - {reminderTime}");
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

                    // 付箋を表示
                    note.Show();
                }

                System.Diagnostics.Debug.WriteLine($"付箋復元完了: {restoredCount}件, リマインダー復元: {reminderRestoredCount}件");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"付箋復元エラー: {ex.Message}");
                MessageBox.Show($"付箋の復元中にエラーが発生しました:\n{ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 重要：すべての復元処理が完了したらグローバルフラグを下ろす
                Common.EndRestore();

                // UIが落ち着いた後で保存を有効化
                this.BeginInvoke(new Action(() =>
                {
                    Common.EnableSave();
                }));
            }
        }
        /// <summary>
        /// 新しい付箋を作成
        /// </summary>
        private void OnCreateNoteClicked(object sender, EventArgs e)
        {
            StickyNoteForm note = new StickyNoteForm();
            note.Location = new Point(
                Cursor.Position.X - CURSOR_OFFSET,
                Cursor.Position.Y - CURSOR_OFFSET
            ); // カーソル位置に表示

            // 付箋をリストに追加
            allNotes.Add(note);

            // 付箋が閉じられたらリストから削除
            note.FormClosed += (s, ev) =>
            {
                allNotes.Remove(note);
            };

            note.Show();
        }

        /// <summary>
        /// すべての付箋を表示 / 非表示
        /// </summary>
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
                ToggleAllNotesMenuItem.Text = "すべての付箋を表示";
            }
            else
            {
                // 表示する
                foreach (var note in allNotes)
                {
                    note.Show();
                }
                allNotesVisible = true;
                ToggleAllNotesMenuItem.Text = "すべての付箋を非表示";
            }
        }



        /// <summary>
        /// データベース整合性チェック
        /// </summary>
        private void OnDatabaseIntegrityCheckClicked(object sender, EventArgs e)
        {
            try
            {
                // 確認ダイアログを表示
                var result = MessageBox.Show(
                    "データベースの整合性チェックと修正を実行します。\n\n" +
                    "実行しますか？",
                    "データベース整合性チェック",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    // 整合性チェックと修正を実行
                    DatabaseIntegrityChecker.CheckAndRepair();

                    // 詳細レポートを表示するか確認
                    var reportResult = MessageBox.Show(
                        "整合性チェックが完了しました。\n\n" +
                        "詳細レポートを表示しますか？",
                        "完了",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information
                    );

                    if (reportResult == DialogResult.Yes)
                    {
                        DatabaseIntegrityChecker.GenerateReport();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"整合性チェック中にエラーが発生しました:\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// 設定（未実装）
        /// </summary>
        private void OnSettingClicked(object sender, EventArgs e)
        {
            MessageBox.Show("設定画面は準備中です。", "設定", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                TrayIcon.ShowBalloonTip(TRAY_BALLOON_TIP_DURATION, "付箋アプリ", "ショートカットキー:\nCtrl+Shift+N: 新しい付箋\nCtrl+Shift+H: 付箋の表示/非表示", ToolTipIcon.Info);
            }

            this.Hide(); // フォームを非表示にする
        }

        /// <summary>
        /// Windowsメッセージを処理（ホットキー用） 不要？なためコメントアウト ショートカットキー実装時に確認
        /// </summary>
        //protected override void WndProc(ref Message m)
        //{
        //    const int WM_HOTKEY = 0x0312;

        //    if (m.Msg == WM_HOTKEY)
        //    {
        //        int hotkeyId = m.WParam.ToInt32();
        //        hotkeyManager.ProcessHotkey(hotkeyId);
        //    }

        //    base.WndProc(ref m);
        //}

        /// <summary>
        /// フォームクローズ時の処理
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // ホットキーを解除
            if (HotkeyManager != null)
            {
                HotkeyManager.UnregisterHotkeys();
            }
        }
    }
}