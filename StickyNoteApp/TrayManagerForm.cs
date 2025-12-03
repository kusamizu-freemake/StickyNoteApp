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
        private static NotifyIcon trayIcon; // タスクトレイアイコン
        private ContextMenuStrip trayMenu; // トレイメニュー
        private ToolStripMenuItem toggleAllNotesMenuItem; // すべての付箋表示/非表示メニュー

        // 全付箋を追跡するリスト
        private List<StickyNoteForm> allNotes = new List<StickyNoteForm>();
        private bool allNotesVisible = true;

        // ホットキーマネージャー
        private HotkeyManager hotkeyManager;

        // 常駐開始
        public TrayManagerForm()
        {
            System.Diagnostics.Debug.WriteLine("TrayManagerForm: コンストラクタ開始");

            InitializeComponent(); // フォームデザイナーで設定したUI要素の初期化

            System.Diagnostics.Debug.WriteLine("TrayManagerForm: InitializeComponent完了");

            InitializeTray(); // トレイアイコン初期化

            System.Diagnostics.Debug.WriteLine("TrayManagerForm: トレイアイコン初期化完了");

            // ホットキーマネージャーの初期化
            hotkeyManager = new HotkeyManager();
            hotkeyManager.NewNoteRequested += (s, e) => OnCreateNoteClicked(s, e);
            hotkeyManager.ToggleNotesRequested += (s, e) => OnShowHideAllStickyNotesClicked(s, e);

            System.Diagnostics.Debug.WriteLine("TrayManagerForm: ホットキーマネージャー初期化完了");

            RestoreNotes(); // 付箋を復元

            System.Diagnostics.Debug.WriteLine("TrayManagerForm: 付箋復元完了");
        }

        /// <summary>
        /// タスクトレイの初期化
        /// </summary>
        private void InitializeTray()
        {
            // 右クリック時に表示されるメニュー
            trayMenu = new ContextMenuStrip();

            // メニュー項目にショートカットキーを表示
            var newNoteItem = new ToolStripMenuItem("新しい付箋を作成");
            newNoteItem.ShortcutKeyDisplayString = "Ctrl+Shift+N";
            newNoteItem.Click += OnCreateNoteClicked;
            trayMenu.Items.Add(newNoteItem);

            toggleAllNotesMenuItem = new ToolStripMenuItem("すべての付箋を非表示");
            toggleAllNotesMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift+H";
            toggleAllNotesMenuItem.Click += OnShowHideAllStickyNotesClicked;
            trayMenu.Items.Add(toggleAllNotesMenuItem);
            
            // DB整合性チェック（デバッグ用。のちに削除予定）
            trayMenu.Items.Add(new ToolStripSeparator()); // 区切り線
            trayMenu.Items.Add("データベース整合性チェック", null, OnDatabaseIntegrityCheckClicked);

            // 設定（未実装）
            trayMenu.Items.Add("設定", null, OnSettingClicked);
            trayMenu.Items.Add(new ToolStripSeparator()); // 区切り線
            trayMenu.Items.Add("アプリを終了", null, OnExitClicked);

            // タスクトレイアイコンの設定
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                ContextMenuStrip = trayMenu,
                Text = "付箋アプリ\nCtrl+Shift+N: 新規付箋\nCtrl+Shift+H: 表示/非表示"
            };

            trayIcon.MouseClick += TrayIcon_MouseClick;
        }

        /// <summary>
        /// トレイアイコン左クリック時の処理（デバッグ用）
        /// </summary>
        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                trayIcon.ShowBalloonTip(1000, "付箋アプリ", "タスクトレイで動作中", ToolTipIcon.Info);
            }
        }

        /// <summary>
        /// 保存された付箋を復元
        /// </summary>
        private void RestoreNotes()
        {
            // 復元した付箋の数をカウント
            int restoredCount = 0;

            try
            {
                System.Diagnostics.Debug.WriteLine("=== 付箋復元処理開始 ===");

                using (SqliteDataReader reader = Database.LoadAll())
                {
                    while (reader.Read())
                    {
                        restoredCount++;

                        string id = reader["Id"].ToString();
                        string content = reader["Content"].ToString();
                        int posX = Convert.ToInt32(reader["PosX"]);
                        int posY = Convert.ToInt32(reader["PosY"]);

                        System.Diagnostics.Debug.WriteLine($"復元中 #{restoredCount}: ID={id}, Text={content}");

                        // 付箋フォームを作成
                        StickyNoteForm note = new StickyNoteForm();

                        // データベースから値を復元
                        note.NoteId = id;
                        note.SetText(content);
                        note.Location = new Point(posX, posY);
                        note.Size = new Size(
                            Convert.ToInt32(reader["Width"]),
                            Convert.ToInt32(reader["Height"])
                        );
                        // 色を復元
                        Color bgColor = Color.FromArgb(
                            Convert.ToInt32(reader["BgR"]),
                            Convert.ToInt32(reader["BgG"]),
                            Convert.ToInt32(reader["BgB"])
                        );

                        // フォームとテキストボックスの背景色を設定
                        note.BackColor = bgColor;
                        note.txtNote.BackColor = bgColor;
                       
                        // TopMostを復元（SetTopMostメソッドを使用）
                        bool topMost = Convert.ToInt32(reader["TopMostFlag"]) == 1;
                        note.SetTopMost(topMost);

                        note.CreatedAt = reader["CreatedAt"].ToString();

                        // 付箋をリストに追加
                        allNotes.Add(note);

                        // 付箋が閉じられたらリストから削除
                        note.FormClosed += (s, e) =>
                        {
                            allNotes.Remove(note);
                            System.Diagnostics.Debug.WriteLine($"付箋削除: 残り{allNotes.Count}件");
                        };

                        // 付箋を表示
                        note.Show();
                    }
                }

                System.Diagnostics.Debug.WriteLine($"=== 付箋復元完了: {restoredCount}件 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"復元エラー: {ex.Message}");
                MessageBox.Show($"付箋の復元中にエラー:\n{ex.Message}", "エラー");
            }
        }

        /// <summary>
        /// 新しい付箋を作成
        /// </summary>
        private void OnCreateNoteClicked(object sender, EventArgs e)
        {
            StickyNoteForm note = new StickyNoteForm();
            note.Location = new Point(Cursor.Position.X - 50, Cursor.Position.Y - 50);

            // 付箋をリストに追加
            allNotes.Add(note);

            // 付箋が閉じられたらリストから削除
            note.FormClosed += (s, ev) =>
            {
                allNotes.Remove(note);
                System.Diagnostics.Debug.WriteLine($"付箋削除: 残り{allNotes.Count}件");
            };

            note.Show();

            System.Diagnostics.Debug.WriteLine($"新しい付箋作成: ID={note.NoteId}");
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
                toggleAllNotesMenuItem.Text = "すべての付箋を表示";
                System.Diagnostics.Debug.WriteLine($"すべての付箋を非表示にしました ({allNotes.Count}件)");
            }
            else
            {
                // 表示する
                foreach (var note in allNotes)
                {
                    note.Show();
                }
                allNotesVisible = true;
                toggleAllNotesMenuItem.Text = "すべての付箋を非表示";
                System.Diagnostics.Debug.WriteLine($"すべての付箋を表示しました ({allNotes.Count}件)");
            }
        }

        /// <summary>
        /// データベース整合性チェック
        /// </summary>
        private void OnDatabaseIntegrityCheckClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("データベース整合性チェック開始");

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
                System.Diagnostics.Debug.WriteLine($"整合性チェックエラー: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine("設定メニューがクリックされました");
        }

        /// <summary>
        /// アプリケーション終了
        /// </summary>
        private void OnExitClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("アプリケーション終了");
            trayIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// 起動時にフォームを表示しない（タスクトレイのみ）
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            System.Diagnostics.Debug.WriteLine("OnShown: フォームを非表示にします");

            // ホットキーを登録
            if (hotkeyManager.RegisterHotkeys(this.Handle))
            {
                System.Diagnostics.Debug.WriteLine("ホットキー登録成功");
                trayIcon.ShowBalloonTip(2000, "付箋アプリ", "ショートカットキー:\nCtrl+Shift+N: 新しい付箋\nCtrl+Shift+H: 付箋の表示/非表示", ToolTipIcon.Info);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ホットキー登録失敗");
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
                hotkeyManager.ProcessHotkey(hotkeyId);
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// フォームクローズ時の処理
        /// 
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // ホットキーを解除
            if (hotkeyManager != null)
            {
                hotkeyManager.UnregisterHotkeys();
                System.Diagnostics.Debug.WriteLine("ホットキー解除完了");
            }
        }
    }
}