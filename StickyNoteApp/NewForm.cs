using Microsoft.Data.Sqlite;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// タスクトレイ管理フォーム
    /// </summary>
    public partial class NewForm : Form
    {
        private static NotifyIcon trayIcon; // タスクトレイアイコン
        private ContextMenuStrip trayMenu; // トレイメニュー

        public NewForm()
        {
            InitializeComponent();

            try
            {
                Database.Initialize(); // データベース初期化
                System.Diagnostics.Debug.WriteLine("データベース初期化成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データベース初期化エラー:\n{ex.Message}", "エラー");
            }

            InitializeTray(); // トレイアイコン初期化
            RestoreNotes(); // ★ここで復元処理を呼び出す
        }

        /// <summary>
        /// タスクトレイの初期化
        /// </summary>
        private void InitializeTray()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("新しい付箋を作成", null, OnCreateNoteClicked);
            trayMenu.Items.Add("アプリを終了", null, OnExitClicked);

            // タスクトレイアイコンの設定
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                ContextMenuStrip = trayMenu,
                Text = "付箋アプリ"
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
                        note.BackColor = Color.FromArgb(
                            Convert.ToInt32(reader["BgR"]),
                            Convert.ToInt32(reader["BgG"]),
                            Convert.ToInt32(reader["BgB"])
                        );
                        note.TopMost = Convert.ToInt32(reader["TopMostFlag"]) == 1;
                        note.CreatedAt = reader["CreatedAt"].ToString();

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
            note.Show();

            System.Diagnostics.Debug.WriteLine($"新しい付箋作成: ID={note.NoteId}");
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
            this.Hide();
        }
    }
}