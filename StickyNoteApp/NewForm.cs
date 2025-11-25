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
            InitializeTray();
        }

        /// <summary>
        /// タスクトレイの初期化
        /// </summary>
        private void InitializeTray()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("新しい付箋を作成", null, OnCreateNoteClicked);
            trayMenu.Items.Add("アプリを終了", null, OnExitClicked);

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
        /// 新しい付箋を作成
        /// </summary>
        private void OnCreateNoteClicked(object sender, EventArgs e)
        {
            StickyNoteForm note = new StickyNoteForm();
            note.Location = new Point(Cursor.Position.X - 50, Cursor.Position.Y - 50);
            note.Show();
        }

        /// <summary>
        /// アプリケーション終了
        /// </summary>
        private void OnExitClicked(object sender, EventArgs e)
        {
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