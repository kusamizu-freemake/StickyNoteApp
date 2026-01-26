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
        // 定数定義

        // リサイズ関連
        private const int RESIZE_BORDER_WIDTH = 8; // リサイズ可能な枠の幅
        private const int MIN_WIDTH = 150; // 最小幅
        private const int MIN_HEIGHT = 100; // 最小高さ

        // 新規付箋作成時のオフセット
        private const int NEW_NOTE_OFFSET_X = 30; // 新規付箋のX方向オフセット
        private const int NEW_NOTE_OFFSET_Y = 30; // 新規付箋のY方向オフセット

        // 画像表示関連
        private const int IMAGE_SIZE_SMALL = 100; // 画像サイズ：小
        private const int IMAGE_SIZE_MEDIUM = 150; // 画像サイズ：中
        private const int IMAGE_SIZE_LARGE = 200; // 画像サイズ：大
        private const int IMAGE_SIZE_EXTRA_LARGE = 250; // 画像サイズ：特大

        // テキストプレビュー関連
        private const int PREVIEW_TEXT_MAX_LENGTH = 20; // プレビューテキストの最大文字数

        // リマインダー時間（分）
        private const int REMINDER_TIME_10MIN = 10; // 10分
        private const int REMINDER_TIME_30MIN = 30; // 30分
        private const int REMINDER_TIME_60MIN = 60; // 60分

        // ドラッグ移動用の変数
        private bool dragging = false;
        private Point dragStart;

        // サイズ変更用の変数
        private bool resizing = false;
        private Point resizeStart;
        private Size resizeStartSize;
        private Point resizeStartLocation;
        private ResizeDirection resizeDirection = ResizeDirection.None;

        // サイズ変更の方向
        private enum ResizeDirection
        {
            None,
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        // 付箋ID
        public string NoteId { get; set; } = Guid.NewGuid().ToString();

        // 作成日時
        public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // リマインダーマネージャー
        private ReminderManager reminderManager;

        // 画像管理
        private PictureBox pictureBox;
        private ImageManager imageManager;

        // データベース保存用プロパティ（ImageManagerへの委譲）
        public string OriginalImagePath => imageManager?.OriginalImagePath; // 元画像のパス（データベース保存用）
        public string ResizedImagePath => imageManager?.ResizedImagePath; // リサイズ済み画像のパス（データベース保存用）
        public int ImageDisplayHeight => imageManager?.ImageDisplayHeight ?? 150; // 画像表示高さ（データベース保存用）
        public string CapturedImagePath => imageManager?.CapturedImagePath; // キャプチャ画像のパス（互換性のため残す。）

        public StickyNoteForm()
        {
            InitializeComponent();            // フォームデザイナーで設定したUI要素の初期化
            InitializeEventHandlers();        // 付箋内容変更検知用のイベントハンドラーの初期化
            InitializeResizeHandlers();       // サイズ変更ハンドラーの初期化（リサイズ機能）
            InitializeReminder();             // リマインダー管理の初期化
            InitializePictureBox();           // 画像表示用のPictureBox初期化

            // 画像マネージャーの初期化
            imageManager = new ImageManager(this, pictureBox);

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
        ///</summary>
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
        /// サイズ変更ハンドラーの初期化（リサイズ機能）
        /// </summary>
        private void InitializeResizeHandlers()
        {
            // フォーム全体のマウスイベント
            this.MouseMove += Form_MouseMove;
            this.MouseDown += Form_MouseDown;
            this.MouseUp += Form_MouseUp;

            // テキストボックスのマウスイベントも処理（Dock=Fillのため）
            // ただし、枠の近くのみ処理するように変更
            this.txtNote.MouseMove += TxtNote_MouseMove;
            this.txtNote.MouseDown += TxtNote_MouseDown;
            this.txtNote.MouseUp += TxtNote_MouseUp;
        }

        /// <summary>
        /// テキストボックスのマウス移動（サイズ変更用）
        /// </summary>
        private void TxtNote_MouseMove(object sender, MouseEventArgs e)
        {
            // テキストボックス内の座標をフォーム座標に変換
            Point formPoint = this.txtNote.PointToScreen(e.Location);
            formPoint = this.PointToClient(formPoint);

            if (resizing)
            {
                PerformResize(formPoint);
            }
            else
            {
                // 枠の近くにいる場合のみカーソルを変更
                ResizeDirection direction = GetResizeDirection(formPoint);
                if (direction != ResizeDirection.None)
                {
                    UpdateCursorForTextBox(formPoint);
                }
                else
                {
                    // 枠から離れている場合は通常のカーソル
                    this.txtNote.Cursor = Cursors.IBeam;
                }
            }
        }

        /// <summary>
        /// テキストボックスのマウスダウン（サイズ変更開始）
        /// </summary>
        private void TxtNote_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point formPoint = this.txtNote.PointToScreen(e.Location);
                formPoint = this.PointToClient(formPoint);

                resizeDirection = GetResizeDirection(formPoint);

                // 枠の近くでのみサイズ変更を開始
                if (resizeDirection != ResizeDirection.None)
                {
                    resizing = true;
                    resizeStart = formPoint;
                    resizeStartSize = this.Size;
                    resizeStartLocation = this.Location;
                }
            }
        }

        /// <summary>
        /// テキストボックスのマウスアップ（サイズ変更終了）
        /// </summary>
        private void TxtNote_MouseUp(object sender, MouseEventArgs e)
        {
            if (resizing)
            {
                resizing = false;
                resizeDirection = ResizeDirection.None;

                // サイズ変更完了時に保存
                SaveCurrentNoteState();

                // テキスト選択を再有効化
                this.txtNote.Focus();
                this.txtNote.Cursor = Cursors.IBeam;
            }
        }

        /// <summary>
        /// テキストボックス用のカーソル更新
        /// </summary>
        private void UpdateCursorForTextBox(Point location)
        {
            ResizeDirection direction = GetResizeDirection(location);

            switch (direction)
            {
                case ResizeDirection.Left:
                case ResizeDirection.Right:
                    this.txtNote.Cursor = Cursors.SizeWE;
                    break;
                case ResizeDirection.Top:
                case ResizeDirection.Bottom:
                    this.txtNote.Cursor = Cursors.SizeNS;
                    break;
                case ResizeDirection.TopLeft:
                case ResizeDirection.BottomRight:
                    this.txtNote.Cursor = Cursors.SizeNWSE;
                    break;
                case ResizeDirection.TopRight:
                case ResizeDirection.BottomLeft:
                    this.txtNote.Cursor = Cursors.SizeNESW;
                    break;
                default:
                    this.txtNote.Cursor = Cursors.IBeam;
                    break;
            }
        }

        /// <summary>
        /// マウス移動時の処理（カーソル変更とサイズ変更）
        /// </summary>
        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (resizing)
            {
                // サイズ変更中
                PerformResize(e.Location);
            }
            else
            {
                // カーソルの形状を変更
                UpdateCursor(e.Location);
            }
        }

        /// <summary>
        /// マウスダウン時の処理（サイズ変更開始）
        /// </summary>
        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                resizeDirection = GetResizeDirection(e.Location);
                if (resizeDirection != ResizeDirection.None)
                {
                    resizing = true;
                    resizeStart = e.Location;
                    resizeStartSize = this.Size;
                    resizeStartLocation = this.Location;
                }
            }
        }

        /// <summary>
        /// マウスアップ時の処理（サイズ変更終了）
        /// </summary>
        private void Form_MouseUp(object sender, MouseEventArgs e)
        {
            if (resizing)
            {
                resizing = false;
                resizeDirection = ResizeDirection.None;

                // サイズ変更完了時に保存
                SaveCurrentNoteState();
            }
        }

        /// <summary>
        /// マウス位置からサイズ変更の方向を判定
        /// </summary>
        private ResizeDirection GetResizeDirection(Point location)
        {
            // フォーム全体のサイズに基づいて判定
            int formWidth = this.ClientSize.Width;
            int formHeight = this.ClientSize.Height;

            bool isLeft = location.X <= RESIZE_BORDER_WIDTH;
            bool isRight = location.X >= formWidth - RESIZE_BORDER_WIDTH;
            bool isTop = location.Y <= RESIZE_BORDER_WIDTH;
            bool isBottom = location.Y >= formHeight - RESIZE_BORDER_WIDTH;

            // 角の判定（優先度高）
            if (isTop && isLeft)
                return ResizeDirection.TopLeft;
            else if (isTop && isRight)
                return ResizeDirection.TopRight;
            else if (isBottom && isLeft)
                return ResizeDirection.BottomLeft;
            else if (isBottom && isRight)
                return ResizeDirection.BottomRight;
            // 辺の判定
            else if (isLeft)
                return ResizeDirection.Left;
            else if (isRight)
                return ResizeDirection.Right;
            else if (isTop)
                return ResizeDirection.Top;
            else if (isBottom)
                return ResizeDirection.Bottom;
            else
                return ResizeDirection.None;
        }

        /// <summary>
        /// カーソルの形状を更新
        /// </summary>
        private void UpdateCursor(Point location)
        {
            // サイズ変更可能な領域にいるか判定
            ResizeDirection direction = GetResizeDirection(location);

            switch (direction)
            {
                case ResizeDirection.Left:
                case ResizeDirection.Right:
                    this.Cursor = Cursors.SizeWE;
                    break;
                case ResizeDirection.Top:
                case ResizeDirection.Bottom:
                    this.Cursor = Cursors.SizeNS;
                    break;
                case ResizeDirection.TopLeft:
                case ResizeDirection.BottomRight:
                    this.Cursor = Cursors.SizeNWSE;
                    break;
                case ResizeDirection.TopRight:
                case ResizeDirection.BottomLeft:
                    this.Cursor = Cursors.SizeNESW;
                    break;
                default:
                    this.Cursor = Cursors.Default;
                    break;
            }
        }

        /// <summary>
        /// サイズ変更を実行
        /// </summary>
        private void PerformResize(Point currentLocation)
        {
            int deltaX = currentLocation.X - resizeStart.X;
            int deltaY = currentLocation.Y - resizeStart.Y;

            int newWidth = resizeStartSize.Width;
            int newHeight = resizeStartSize.Height;
            int newLeft = resizeStartLocation.X;
            int newTop = resizeStartLocation.Y;

            switch (resizeDirection)
            {
                // 各方向ごとのサイズと位置の計算
                // 右方向にリサイズ
                case ResizeDirection.Right:
                    newWidth = Math.Max(MIN_WIDTH, resizeStartSize.Width + deltaX);
                    break;

                // 左方向にリサイズ
                case ResizeDirection.Left:
                    newWidth = Math.Max(MIN_WIDTH, resizeStartSize.Width - deltaX);
                    if (newWidth > MIN_WIDTH)
                    {
                        newLeft = resizeStartLocation.X + deltaX;
                    }
                    break;

                // 下方向にリサイズ
                case ResizeDirection.Bottom:
                    newHeight = Math.Max(MIN_HEIGHT, resizeStartSize.Height + deltaY);
                    break;

                // 上方向にリサイズ
                case ResizeDirection.Top:
                    newHeight = Math.Max(MIN_HEIGHT, resizeStartSize.Height - deltaY);
                    if (newHeight > MIN_HEIGHT)
                    {
                        newTop = resizeStartLocation.Y + deltaY;
                    }
                    break;

                // 右下方向にリサイズ
                case ResizeDirection.BottomRight:
                    newWidth = Math.Max(MIN_WIDTH, resizeStartSize.Width + deltaX);
                    newHeight = Math.Max(MIN_HEIGHT, resizeStartSize.Height + deltaY);
                    break;

                // 左下方向にリサイズ
                case ResizeDirection.BottomLeft:
                    newWidth = Math.Max(MIN_WIDTH, resizeStartSize.Width - deltaX);
                    newHeight = Math.Max(MIN_HEIGHT, resizeStartSize.Height + deltaY);
                    if (newWidth > MIN_WIDTH)
                    {
                        newLeft = resizeStartLocation.X + deltaX;
                    }
                    break;

                // 右上方向にリサイズ
                case ResizeDirection.TopRight:
                    newWidth = Math.Max(MIN_WIDTH, resizeStartSize.Width + deltaX);
                    newHeight = Math.Max(MIN_HEIGHT, resizeStartSize.Height - deltaY);
                    if (newHeight > MIN_HEIGHT)
                    {
                        newTop = resizeStartLocation.Y + deltaY;
                    }
                    break;

                // 左上方向にリサイズ
                case ResizeDirection.TopLeft:
                    newWidth = Math.Max(MIN_WIDTH, resizeStartSize.Width - deltaX);
                    newHeight = Math.Max(MIN_HEIGHT, resizeStartSize.Height - deltaY);
                    if (newWidth > MIN_WIDTH)
                    {
                        newLeft = resizeStartLocation.X + deltaX;
                    }
                    if (newHeight > MIN_HEIGHT)
                    {
                        newTop = resizeStartLocation.Y + deltaY;
                    }
                    break;
            }

            // 位置とサイズを同時に更新
            this.SetBounds(newLeft, newTop, newWidth, newHeight);
        }

        /// <summary>
        /// リマインダーの初期化
        /// </summary>
        private void InitializeReminder()
        {
            // 自分自身（this）をReminderManagerに渡す
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
        /// ReminderManagerから呼ばれる保存メソッド
        /// </summary>
        public void SaveCurrentNoteState()
        {
            // グローバル復元フラグをチェック
            if (Common.IsRestoring) return;

            try
            {
                // 現在の付箋情報をすべてDatabase.SaveOrUpdate()に渡す
                Database.SaveOrUpdate(this);

                string preview = string.IsNullOrEmpty(txtNote.Text) ? "(空)" :
                    (txtNote.Text.Length > PREVIEW_TEXT_MAX_LENGTH ?
                     txtNote.Text.Substring(0, PREVIEW_TEXT_MAX_LENGTH) + "..." :
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

        // DBから読み込んだリマインダー情報を設定
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
        /// 付箋に貼り付けた画像を表示する PictureBoxの初期化
        /// </summary>
        private void InitializePictureBox()
        {
            pictureBox = new PictureBox();
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.Dock = DockStyle.Top;
            pictureBox.Height = 0;
            pictureBox.Visible = false;
            pictureBox.BackColor = Color.White;
            pictureBox.BorderStyle = BorderStyle.FixedSingle;

            // ダブルクリックで画像を削除
            pictureBox.DoubleClick += PictureBox_DoubleClick;

            // 右クリックメニューを追加
            var imageContextMenu = new ContextMenuStrip();

            var deleteImageItem = new ToolStripMenuItem("画像を削除");
            deleteImageItem.Click += (s, e) => imageManager?.RemoveImage();
            imageContextMenu.Items.Add(deleteImageItem);
            imageContextMenu.Items.Add(new ToolStripSeparator());

            var copyImageItem = new ToolStripMenuItem("画像をコピー");
            copyImageItem.Click += (s, e) => imageManager?.CopyImage();
            imageContextMenu.Items.Add(copyImageItem);

            var saveImageAsItem = new ToolStripMenuItem("画像を名前を付けて保存");
            saveImageAsItem.Click += (s, e) => imageManager?.SaveImageAs();
            imageContextMenu.Items.Add(saveImageAsItem);

            imageContextMenu.Items.Add(new ToolStripSeparator());

            var resizeImageItem = new ToolStripMenuItem("画像サイズ");

            var sizeSmallItem = new ToolStripMenuItem("小 (100px)");
            sizeSmallItem.Click += (s, e) => imageManager?.ResizeImage(IMAGE_SIZE_SMALL);
            resizeImageItem.DropDownItems.Add(sizeSmallItem);

            var sizeMediumItem = new ToolStripMenuItem("中 (150px)");
            sizeMediumItem.Click += (s, e) => imageManager?.ResizeImage(IMAGE_SIZE_MEDIUM);
            resizeImageItem.DropDownItems.Add(sizeMediumItem);

            var sizeLargeItem = new ToolStripMenuItem("大 (200px)");
            sizeLargeItem.Click += (s, e) => imageManager?.ResizeImage(IMAGE_SIZE_LARGE);
            resizeImageItem.DropDownItems.Add(sizeLargeItem);

            var sizeExtraLargeItem = new ToolStripMenuItem("特大 (250px)");
            sizeExtraLargeItem.Click += (s, e) => imageManager?.ResizeImage(IMAGE_SIZE_EXTRA_LARGE);
            resizeImageItem.DropDownItems.Add(sizeExtraLargeItem);

            imageContextMenu.Items.Add(resizeImageItem);

            pictureBox.ContextMenuStrip = imageContextMenu;

            // txtNoteの前に追加（タイトルバーの下）
            this.Controls.Add(pictureBox);
            // pictureBox を txtNote より前（上）に移動
            this.Controls.SetChildIndex(pictureBox, 0);
        }


        /// <summary>
        /// PictureBoxダブルクリック時の処理（画像削除）
        /// </summary>
        private void PictureBox_DoubleClick(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "画像を削除しますか？",
                "確認",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                imageManager?.RemoveImage();
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

                // ドラッグ移動完了時に保存
                SaveCurrentNoteState();

            }
        }

        /// <summary>
        /// 閉じるボタンクリック時の処理
        /// </summary>
        private void Button_Close_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("この付箋を削除しますか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
            newNote.Location = new Point(this.Left + NEW_NOTE_OFFSET_X, this.Top + NEW_NOTE_OFFSET_Y);
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
        /// 右クリックメニュー：付箋削除処理
        /// </summary>
        private void StickyNoteMenu_Delete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("この付箋を削除しますか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Database.SoftDelete(NoteId);
                this.Close();
            }
        }

        /// <summary>
        /// 色を変更する共通処理 共通機能としてファイル分ける予定
        /// </summary>
        private void ChangeColor(Color color)
        {
            // BackColorChangedイベントを一時的に解除
            this.BackColorChanged -= BackColor_Changed;
            // テキストボックスの色のみを変更
            this.txtNote.BackColor = color;
            // データベース保存用にフォームのBackColorも更新
            this.BackColor = color;
            // タイトルバーの色を確実に固定（念のため再設定）
            this.titleBar.BackColor = Color.WhiteSmoke;
            // イベントを再登録
            this.BackColorChanged += BackColor_Changed;

            // 色変更完了時に保存
            SaveCurrentNoteState();
        }

        /// <summary>
        /// 色メニュー：イエロー
        /// </summary>
        private void SelectColor_Yellow_Click(object sender, EventArgs e)
        {
            ChangeColor(Color.Khaki);
        }

        /// <summary>
        /// 色メニュー：ピンク
        /// </summary>
        private void SelectColor_Pink_Click(object sender, EventArgs e)
        {
            ChangeColor(Color.LightPink);
        }

        /// <summary>
        /// 色メニュー：ブルー
        /// </summary>
        private void SelectColor_Blue_Click(object sender, EventArgs e)
        {
            ChangeColor(Color.LightBlue);
        }

        /// <summary>
        /// 色メニュー：グリーン
        /// </summary>
        private void SelectColor_Green_Click(object sender, EventArgs e)
        {
            ChangeColor(Color.LightGreen);
        }

        /// <summary>
        /// 色メニュー：オレンジ
        /// </summary>
        private void SelectColor_Orange_Click(object sender, EventArgs e)
        {
            ChangeColor(Color.LightSalmon);
        }

        /// <summary>
        /// 色メニュー：パープル
        /// </summary>
        private void SelectColor_Purple_Click(object sender, EventArgs e)
        {
            ChangeColor(Color.Plum);
        }


        /// <summary>
        /// クリップボードから画像を貼り付け
        /// </summary>
        private void PasteImageMenuItem_Click(object sender, EventArgs e)
        {
            imageManager?.PasteImageFromClipboard();
        }


        /// <summary>
        /// 画像を復元（データベースから読み込み時用）
        /// （データベースから読み込んだパスを元に表示）
        /// </summary>
        public void LoadCapturedImage(string imagePath, string originalPath, string resizedPath, int displayHeight)
        {
            imageManager?.LoadImage(imagePath, originalPath, resizedPath, displayHeight);
        }

        /// <summary>
        /// リマインダーメニュー:カスタム時間指定
        /// </summary>
        private void ReminderCustomMenuItem_Click(object sender, EventArgs e)
        {
            reminderManager?.ShowCustomReminderDialog(NoteId, txtNote.Text);
        }

        /// <summary>
        /// リマインダーメニュー:10分後
        /// </summary>
        private void Reminder10MinMenuItem_Click(object sender, EventArgs e)
        {
            reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, REMINDER_TIME_10MIN);
        }

        /// <summary>
        /// リマインダーメニュー:30分後
        /// </summary>
        private void Reminder30MinMenuItem_Click(object sender, EventArgs e)
        {
            reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, REMINDER_TIME_30MIN);
        }

        /// <summary>
        /// リマインダーメニュー:60分後
        /// </summary>
        private void Reminder60MinMenuItem_Click(object sender, EventArgs e)
        {
            reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, REMINDER_TIME_60MIN);
        }

        /// <summary>
        /// リマインダーメニュー:キャンセル
        /// </summary>
        private void ReminderCancelMenuItem_Click(object sender, EventArgs e)
        {
            reminderManager?.ShowCancelReminderDialog();
        }

        /// <summary>
        /// テキストを設定（復元時用）
        /// </summary>
        public void SetText(string text)
        {
            txtNote.Text = text;
        }

        /// <summary>
        /// TopMostを設定（復元時用） ウィンドウを他のウィンドウより常に前面に表示する設定
        /// </summary>
        public void SetTopMost(bool topMost)
        {
            this.TopMost = topMost;
            UpdateTopMostMenuState();
        }

        /// <summary>
        /// フォームクローズ時の処理
        /// フォームが閉じられる直前に自動的に呼び出されます（×ボタン、Alt+F4、Close()メソッドなど）。
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
            System.Diagnostics.Debug.WriteLine($"[{NoteId}] 最終保存実行");
            try
            {
                SaveCurrentNoteState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] 最終保存失敗: {ex.Message}");
                // クローズ時のエラーは無視
            }
        }
    }
}