using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// 付箋ウィンドウ
    /// 初期化・保存・プロパティ
    /// </summary>
    public partial class StickyNoteForm : Form
    {
        // 画像管理
        private PictureBox pictureBox;
        private ImageManager imageManager;
        private ImageResizeManager imageResizeManager;
        private ImageResizer imageResizer;

        // 付箋ID
        public string NoteId { get; set; } = Guid.NewGuid().ToString();

        // 作成日時
        public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // リマインダーマネージャー
        private ReminderManager reminderManager;

        // データベース保存用プロパティ（ImageManagerへの委譲）
        public string OriginalImagePath => imageManager?.OriginalImagePath;             // 元画像のパス（データベース保存用）
        public string ResizedImagePath => imageManager?.ResizedImagePath;              // リサイズ済み画像のパス（データベース保存用）
        public int ImageDisplayHeight => imageManager?.ImageDisplayHeight ?? 150;     // 画像表示高さ（データベース保存用）
        public string CapturedImagePath => imageManager?.CapturedImagePath;             // キャプチャ画像のパス（互換性のため残す。）

        public StickyNoteForm()
        {
            InitializeComponent();            // フォームデザイナーで設定したUI要素の初期化
            InitializeEventHandlers();        // 付箋内容変更検知用のイベントハンドラーの初期化
            InitializeResizeHandlers();       // サイズ変更ハンドラーの初期化（リサイズ機能→StickyNoteForm.Resize.cs）
            InitializeReminder();             // リマインダー管理の初期化

            // 画像マネージャーとリサイズマネージャーを先に初期化
            pictureBox = new PictureBox();    // PictureBoxを先に作成
            imageManager = new ImageManager(this, pictureBox);

            // 画像リサイザーの初期化（インスタンス化）
            string imageFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StickyNoteApp",
                "Images");
            imageResizer = new ImageResizer(imageFolder);

            // 画像リサイズマネージャーの初期化（ImageResizerを渡す）
            imageResizeManager = new ImageResizeManager(this, pictureBox, imageManager, imageResizer);

            // この時点で imageManager と imageResizeManager は使用可能
            // 
            InitializePictureBox();

            // 最前面表示の初期状態を反映
            UpdateTopMostMenuState();
        }

        /// <summary>
        /// 付箋内容変更検知用のイベントハンドラーの初期化
        /// </summary>
        private void InitializeEventHandlers()
        {
            // フォームが非アクティブになった時に保存
            this.Deactivate += StickyNoteForm_Deactivate;

            // 背景色変更時：色変更操作完了時に保存
            this.BackColorChanged += BackColor_Changed;
        }

        /// <summary>
        /// 付箋ウィンドウが非アクティブ（フォーカスを失った）になったときに呼ばれる処理。
        /// 復元処理中でなければ、現在の付箋の状態を保存する。
        /// また、最前面表示（TopMost）が無効な場合は、再度付箋を前面に表示する。
        /// </summary>
        private void StickyNoteForm_Deactivate(object sender, EventArgs e)
        {
            // グローバル復元フラグをチェック
            if (Common.IsRestoring) return;

            SaveCurrentNoteState();

            // TopMostが無効な場合、付箋を前面に戻す
            if (!this.TopMost)
            {
                this.BringToFront();
            }
        }

        /// <summary>
        /// 背景色変更時の処理
        /// </summary>
        private void BackColor_Changed(object sender, EventArgs e)
        {
            // グローバル復元フラグをチェック
            if (Common.IsRestoring) return;

            // 色変更完了時に保存
            SaveCurrentNoteState();
        }

        /// <summary>
        /// 付箋ウィンドウが画面に表示されたときに呼ばれる処理
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // 画像ありで起動した場合、テキスト入力欄の位置がズレることがあるため
            // ウィンドウ表示後に正しい位置へ調整し直す
            imageManager?.AdjustTextBoxPosition();
        }

        /// <summary>
        /// リマインダーの初期化
        /// </summary>
        private void InitializeReminder()
        {
            reminderManager = new ReminderManager(this);

            // リマインダー通知イベントの登録
            reminderManager.ReminderTriggered += (s, e) =>
            {
                this.TopMost = true;
                this.Activate();
            };
        }

        /// <summary>
        /// 現在の付箋状態を保存
        /// </summary>
        public void SaveCurrentNoteState()
        {
            // グローバル復元フラグをチェック
            if (Common.IsRestoring) return;

            try
            {
                Database.SaveOrUpdate(this);

                string preview = string.IsNullOrEmpty(txtNote.Text) ? "(空)" :
                    (txtNote.Text.Length > AppConstants.StickyNoteFormConfig.PREVIEW_TEXT_MAX_LENGTH ?
                     txtNote.Text.Substring(0, AppConstants.StickyNoteFormConfig.PREVIEW_TEXT_MAX_LENGTH) + "..." :
                     txtNote.Text);
            }
            catch
            {
                // 連続保存時にエラーダイアログが出続けるのを防ぐため、エラーは無視
            }
        }

        /// <summary>
        /// リマインダーマネージャーを取得
        /// </summary>
        public ReminderManager GetReminderManager()
        {
            return reminderManager;
        }

        /// <summary>
        /// リマインダー情報を取得（データベース保存用）
        /// </summary>
        public ReminderInfo GetReminderInfo()
        {
            if (reminderManager != null)
            {
                return reminderManager.GetReminderInfo();
            }
            return new ReminderInfo { IsActive = false };
        }

        /// <summary>
        /// リマインダーを復元（データベースからの読み込み時用）
        /// </summary>
        public void RestoreReminder(DateTime reminderTime)
        {
            if (reminderManager != null && reminderTime > DateTime.Now)
            {
                reminderManager.RestoreReminder(NoteId, txtNote.Text, reminderTime);
            }
        }

        /// <summary>
        /// 画像を復元（データベースから読み込み時用）
        /// </summary>
        public void LoadCapturedImage(string imagePath, string originalPath, string resizedPath, int displayHeight)
        {
            imageManager?.LoadImage(imagePath, originalPath, resizedPath, displayHeight);
        }

        /// <summary>
        /// テキストを設定（復元時用）
        /// </summary>
        public void SetText(string text)
        {
            txtNote.Text = text;
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
        /// フォームクローズ時の処理
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // リマインダーを先に破棄
            if (reminderManager != null)
            {
                reminderManager.Dispose();
                reminderManager = null;
            }

            // 画像マネージャーのリソースを解放
            if (imageManager != null)
            {
                imageManager.Dispose();
            }

            // フォームクローズ時に最終保存
            System.Diagnostics.Debug.WriteLine(string.Format(AppConstants.StickyNoteFormMsg.MSG_FINAL_SAVE, NoteId));
            try
            {
                SaveCurrentNoteState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format(AppConstants.StickyNoteFormMsg.MSG_FINAL_SAVE_FAIL, NoteId, ex.Message));
                // クローズ時のエラーは無視
            }
        }

        // =========================================================
        // 以下のメソッドは今後の分割予定ファイルに移動予定
        //
        //   StickyNoteForm.ReminderMenu.cs← Reminder*_Click
        //   StickyNoteForm.ContextMenu.cs ← ContextMenu_Opening / Button_Close_Click /
        //                                    StickyNoteMenu_*_Click
        // =========================================================

        /// <summary>
        /// 閉じるボタンクリック時の処理
        /// </summary>
        private void Button_Close_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(AppConstants.StickyNoteFormMsg.MSG_CONFIRM_DELETE, AppConstants.SharedTitle.CONFIRM, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Database.SoftDelete(NoteId);
                this.Close();
            }
        }

        /// <summary>
        /// 右クリックメニュー：新しい付箋を作成
        /// </summary>
        private void StickyNoteMenu_New_Click(object sender, EventArgs e)
        {
            StickyNoteForm newNote = new StickyNoteForm();
            // 現在の付箋の近くに表示
            newNote.Location = new Point(this.Left + AppConstants.StickyNoteFormConfig.NEW_NOTE_OFFSET_X, this.Top + AppConstants.StickyNoteFormConfig.NEW_NOTE_OFFSET_Y);
            newNote.Show();
        }

        /// <summary>
        /// 右クリックメニュー：最前面表示の切り替え
        /// </summary>
        private void StickyNoteMenu_TopMost_Click(object sender, EventArgs e)
        {
            this.TopMost = topMostMenuItem.Checked;
            // 最前面表示切り替え完了時に保存
            SaveCurrentNoteState();
        }

        /// <summary>
        /// 右クリックメニュー：付箋削除処理
        /// </summary>
        private void StickyNoteMenu_Delete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(AppConstants.StickyNoteFormMsg.MSG_CONFIRM_DELETE, AppConstants.SharedTitle.CONFIRM, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Database.SoftDelete(NoteId);
                this.Close();
            }
        }

        /// <summary>
        /// 右クリックメニューが開く前に、画像の有無に応じて削除・サイズ変更メニューを表示/非表示
        /// </summary>
        private void ContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 画像があるかどうかをチェック
            bool hasImage = pictureBox != null && pictureBox.Image != null;
            // 画像がある時だけ表示する（削除・サイズ変更）
            removeImageMenuItem.Visible = hasImage;
            imageSizeMenuItem.Visible = hasImage;
        }


        /// <summary>リマインダーメニュー：カスタム時間指定</summary>
        private void ReminderCustomMenuItem_Click(object sender, EventArgs e)
            => reminderManager?.ShowCustomReminderDialog(NoteId, txtNote.Text);

        /// <summary>リマインダーメニュー：10分後</summary>
        private void Reminder10MinMenuItem_Click(object sender, EventArgs e)
            => reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, AppConstants.StickyNoteFormConfig.REMINDER_TIME_10MIN);

        /// <summary>リマインダーメニュー：30分後</summary>
        private void Reminder30MinMenuItem_Click(object sender, EventArgs e)
            => reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, AppConstants.StickyNoteFormConfig.REMINDER_TIME_30MIN);

        /// <summary>リマインダーメニュー：60分後</summary>
        private void Reminder60MinMenuItem_Click(object sender, EventArgs e)
            => reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, AppConstants.StickyNoteFormConfig.REMINDER_TIME_60MIN);

        /// <summary>リマインダーメニュー：キャンセル</summary>
        private void ReminderCancelMenuItem_Click(object sender, EventArgs e)
            => reminderManager?.ShowCancelReminderDialog();
    }
}