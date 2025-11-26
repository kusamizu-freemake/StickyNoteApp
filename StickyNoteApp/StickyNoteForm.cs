using System;
using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// 付箋ウィンドウ
    /// </summary>
    public partial class StickyNoteForm : Form
    {
        private bool dragging = false;
        private Point dragStart;
        private Timer autoSaveTimer;
        private bool needsSave = false;

        public string NoteId { get; set; } = Guid.NewGuid().ToString();
        public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public StickyNoteForm()
        {
            InitializeComponent();
            InitializeAutoSave();

            // 最前面表示の初期状態を反映
            UpdateTopMostMenuState();

            System.Diagnostics.Debug.WriteLine($"付箋作成: ID={NoteId}");
        }

        /// <summary>
        /// 自動保存タイマーの初期化
        /// </summary>
        private void InitializeAutoSave()
        {
            // タイマー設定
            autoSaveTimer = new Timer();
            autoSaveTimer.Interval = 1000; // 1秒ごと
            autoSaveTimer.Tick += AutoSaveTimer_Tick;
            autoSaveTimer.Start();

            // イベントハンドラー登録
            this.txtNote.TextChanged += OnContentChanged;
            this.LocationChanged += OnContentChanged;
            this.SizeChanged += OnContentChanged;
            this.BackColorChanged += OnContentChanged;
        }

        /// <summary>
        /// 内容変更時の処理
        /// </summary>
        private void OnContentChanged(object sender, EventArgs e)
        {
            needsSave = true;
            System.Diagnostics.Debug.WriteLine($"[{NoteId}] 変更検出");
        }

        /// <summary>
        /// 自動保存タイマーのティック処理
        /// </summary>
        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            if (needsSave)
            {
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] 自動保存実行中...");
                SaveNote();
                needsSave = false;
            }
        }

        /// <summary>
        /// タイトルバーのマウスダウン
        /// </summary>
        private void MoveForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && sender is Panel)
            {
                dragging = true;
                dragStart = new Point(e.X, e.Y);
            }
        }

        /// <summary>
        /// タイトルバーのマウスムーブ（ドラッグ移動）
        /// </summary>
        private void MoveForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging && e.Button == MouseButtons.Left)
            {
                this.Location = new Point(this.Left + e.X - dragStart.X, this.Top + e.Y - dragStart.Y);
            }
        }

        /// <summary>
        /// タイトルバーのマウスアップ
        /// </summary>
        private void MoveForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                needsSave = true;
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] ドラッグ終了");
            }
        }

        /// <summary>
        /// 閉じるボタンクリック時の処理
        /// </summary>
        private void btnClose_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("この付箋を削除しますか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] 削除実行");
                Database.SoftDelete(NoteId);
                this.Close();
            }
        }
        /// <summary>
        /// 右クリックメニュー：新しい付箋を作成
        /// </summary>
        private void newNoteMenuItem_Click(object sender, EventArgs e)
        {
            StickyNoteForm newNote = new StickyNoteForm();
            // 現在の付箋の近くに表示
            newNote.Location = new Point(this.Left + 30, this.Top + 30);
            newNote.Show();

            System.Diagnostics.Debug.WriteLine($"右クリックから新しい付箋作成: ID={newNote.NoteId}");
        }

        /// <summary>
        /// 右クリックメニュー：最前面表示の切り替え
        /// </summary>
        private void topMostMenuItem_Click(object sender, EventArgs e)
        {
            this.TopMost = topMostMenuItem.Checked;
            needsSave = true;

            System.Diagnostics.Debug.WriteLine($"[{NoteId}] 最前面表示: {this.TopMost}");
        }

        /// <summary>
        /// 最前面表示メニューの状態を更新
        /// </summary>
        private void UpdateTopMostMenuState()
        {
            if (topMostMenuItem != null)
            {
                topMostMenuItem.Checked = this.TopMost;
            }
        }
        /// <summary>
        /// 右クリックメニューの削除処理
        /// </summary>
        private void deleteMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("この付箋を削除しますか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] 削除実行");
                Database.SoftDelete(NoteId);
                this.Close();
            }
        }

        /// <summary>
        /// テキストを設定（復元時用）
        /// </summary>
        public void SetText(string text)
        {
            // イベントを一時的に解除
            this.txtNote.TextChanged -= OnContentChanged;
            txtNote.Text = text;
            // イベントを再登録
            this.txtNote.TextChanged += OnContentChanged;

            System.Diagnostics.Debug.WriteLine($"[{NoteId}] テキスト設定: {text}");
        }
        /// <summary>
        /// TopMostを設定（復元時用）
        /// </summary>
        public void SetTopMost(bool topMost)
        {
            this.TopMost = topMost;
            UpdateTopMostMenuState();
        }
        /// <summary>
        /// 付箋データを保存
        /// </summary>
        private void SaveNote()
        {
            try
            {
                Database.SaveOrUpdate(this);

                string preview = string.IsNullOrEmpty(txtNote.Text) ? "(空)" :
                    (txtNote.Text.Length > 20 ? txtNote.Text.Substring(0, 20) + "..." : txtNote.Text);

                System.Diagnostics.Debug.WriteLine($"✓ 保存成功 [{NoteId}]: '{preview}' at ({Left},{Top})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ 保存エラー [{NoteId}]: {ex.Message}");
                MessageBox.Show($"保存エラー:\n{ex.Message}", "エラー");
            }
        }

        /// <summary>
        /// フォームクローズ時の処理
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            System.Diagnostics.Debug.WriteLine($"[{NoteId}] フォームクローズ");

            // タイマー停止
            if (autoSaveTimer != null)
            {
                autoSaveTimer.Stop();
                autoSaveTimer.Dispose();
            }

            // 最終保存
            if (needsSave)
            {
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] 最終保存実行");
                SaveNote();
            }
        }
    }
}