using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// 付箋ウィンドウ
    /// </summary>
    public partial class StickyNoteForm : Form
    {
        // 画像管理
        private PictureBox pictureBox;
        private ImageManager imageManager;
        private ImageResizeManager imageResizeManager;
        private ImageResizer imageResizer;

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

            bool isLeft = location.X <= AppConstants.StickyNoteFormConfig.RESIZE_BORDER_WIDTH;
            bool isRight = location.X >= formWidth - AppConstants.StickyNoteFormConfig.RESIZE_BORDER_WIDTH;
            bool isTop = location.Y <= AppConstants.StickyNoteFormConfig.RESIZE_BORDER_WIDTH;
            bool isBottom = location.Y >= formHeight - AppConstants.StickyNoteFormConfig.RESIZE_BORDER_WIDTH;

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
                    newWidth = Math.Max(AppConstants.StickyNoteFormConfig.MIN_WIDTH, resizeStartSize.Width + deltaX);
                    break;

                // 左方向にリサイズ
                case ResizeDirection.Left:
                    newWidth = Math.Max(AppConstants.StickyNoteFormConfig.MIN_WIDTH, resizeStartSize.Width - deltaX);
                    if (newWidth > AppConstants.StickyNoteFormConfig.MIN_WIDTH)
                    {
                        newLeft = resizeStartLocation.X + deltaX;
                    }
                    break;

                // 下方向にリサイズ
                case ResizeDirection.Bottom:
                    newHeight = Math.Max(AppConstants.StickyNoteFormConfig.MIN_HEIGHT, resizeStartSize.Height + deltaY);
                    break;

                // 上方向にリサイズ
                case ResizeDirection.Top:
                    newHeight = Math.Max(AppConstants.StickyNoteFormConfig.MIN_HEIGHT, resizeStartSize.Height - deltaY);
                    if (newHeight > AppConstants.StickyNoteFormConfig.MIN_HEIGHT)
                    {
                        newTop = resizeStartLocation.Y + deltaY;
                    }
                    break;

                // 右下方向にリサイズ
                case ResizeDirection.BottomRight:
                    newWidth = Math.Max(AppConstants.StickyNoteFormConfig.MIN_WIDTH, resizeStartSize.Width + deltaX);
                    newHeight = Math.Max(AppConstants.StickyNoteFormConfig.MIN_HEIGHT, resizeStartSize.Height + deltaY);
                    break;

                // 左下方向にリサイズ
                case ResizeDirection.BottomLeft:
                    newWidth = Math.Max(AppConstants.StickyNoteFormConfig.MIN_WIDTH, resizeStartSize.Width - deltaX);
                    newHeight = Math.Max(AppConstants.StickyNoteFormConfig.MIN_HEIGHT, resizeStartSize.Height + deltaY);
                    if (newWidth > AppConstants.StickyNoteFormConfig.MIN_WIDTH)
                    {
                        newLeft = resizeStartLocation.X + deltaX;
                    }
                    break;

                // 右上方向にリサイズ
                case ResizeDirection.TopRight:
                    newWidth = Math.Max(AppConstants.StickyNoteFormConfig.MIN_WIDTH, resizeStartSize.Width + deltaX);
                    newHeight = Math.Max(AppConstants.StickyNoteFormConfig.MIN_HEIGHT, resizeStartSize.Height - deltaY);
                    if (newHeight > AppConstants.StickyNoteFormConfig.MIN_HEIGHT)
                    {
                        newTop = resizeStartLocation.Y + deltaY;
                    }
                    break;

                // 左上方向にリサイズ
                case ResizeDirection.TopLeft:
                    newWidth = Math.Max(AppConstants.StickyNoteFormConfig.MIN_WIDTH, resizeStartSize.Width - deltaX);
                    newHeight = Math.Max(AppConstants.StickyNoteFormConfig.MIN_HEIGHT, resizeStartSize.Height - deltaY);
                    if (newWidth > AppConstants.StickyNoteFormConfig.MIN_WIDTH)
                    {
                        newLeft = resizeStartLocation.X + deltaX;
                    }
                    if (newHeight > AppConstants.StickyNoteFormConfig.MIN_HEIGHT)
                    {
                        newTop = resizeStartLocation.Y + deltaY;
                    }
                    break;
            }

            // 位置とサイズを同時に更新
            this.SetBounds(newLeft, newTop, newWidth, newHeight);

            // フォームサイズが変わったら txtNote（とPictureBox後の領域）を追従させる
            imageManager?.AdjustTextBoxPosition();
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


            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.Dock = DockStyle.Top;
            pictureBox.Height = 0;
            pictureBox.Visible = false;
            pictureBox.BackColor = Color.White;
            pictureBox.BorderStyle = BorderStyle.FixedSingle;

            // ダブルクリックで画像を削除
            pictureBox.DoubleClick += PictureBox_DoubleClick;

            // リサイズ用のマウスイベントハンドラーを追加
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseUp += PictureBox_MouseUp;

            // 右クリックメニューを追加
            var imageContextMenu = new ContextMenuStrip();

            // 画像を編集（置き換え）
            var inserteditImageItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_IMAGE_EDIT);
            inserteditImageItem.Click += (s, e) => imageManager?.SelectAndLoadImageFromFile();
            imageContextMenu.Items.Add(inserteditImageItem);
            imageContextMenu.Items.Add(new ToolStripSeparator()); // 区切り線

            // 画像を削除
            var deleteImageItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_IMAGE_DELETE);
            deleteImageItem.Click += (s, e) => imageManager?.RemoveImage();
            imageContextMenu.Items.Add(deleteImageItem);

            imageContextMenu.Items.Add(new ToolStripSeparator()); // 区切り線

            // 画像サイズ変更サブメニュー
            var resizeImageItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_IMAGE_SIZE);
            imageResizeManager.CreateSizeMenuItems(resizeImageItem);
            imageContextMenu.Items.Add(resizeImageItem);

            // 名前を付けて保存サブメニュー
            var saveAsImageItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_IMAGE_SAVE_AS);

            var saveAsSmallItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_SAVE_SMALL);
            saveAsSmallItem.Click += (s, e) => imageResizeManager?.SaveResizedImageToUserLocation(AppConstants.ImageResizeManagerConfig.SIZE_SMALL);
            saveAsImageItem.DropDownItems.Add(saveAsSmallItem);

            var saveAsMediumItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_SAVE_MEDIUM);
            saveAsMediumItem.Click += (s, e) => imageResizeManager?.SaveResizedImageToUserLocation(AppConstants.ImageResizeManagerConfig.SIZE_MEDIUM);
            saveAsImageItem.DropDownItems.Add(saveAsMediumItem);

            var saveAsLargeItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_SAVE_LARGE);
            saveAsLargeItem.Click += (s, e) => imageResizeManager?.SaveResizedImageToUserLocation(AppConstants.ImageResizeManagerConfig.SIZE_LARGE);
            saveAsImageItem.DropDownItems.Add(saveAsLargeItem);

            var saveAsExtraLargeItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_SAVE_EXTRA_LARGE);
            saveAsExtraLargeItem.Click += (s, e) => imageResizeManager?.SaveResizedImageToUserLocation(AppConstants.ImageResizeManagerConfig.SIZE_EXTRA_LARGE);
            saveAsImageItem.DropDownItems.Add(saveAsExtraLargeItem);

            saveAsImageItem.DropDownItems.Add(new ToolStripSeparator()); // 区切り線

            var saveAsOriginalItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_SAVE_ORIGINAL);
            saveAsOriginalItem.Click += (s, e) => imageResizeManager?.SaveResizedImageToUserLocation(0); // 0は元サイズ
            saveAsImageItem.DropDownItems.Add(saveAsOriginalItem);

            imageContextMenu.Items.Add(saveAsImageItem);

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
                AppConstants.StickyNoteFormMsg.MSG_CONFIRM_REMOVE_IMAGE,
                AppConstants.SharedTitle.CONFIRM,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                imageManager?.RemoveImage();
            }
        }

        /// <summary>
        /// PictureBoxのマウス移動（サイズ変更用）
        /// </summary>
        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            // PictureBox内の座標をフォーム座標に変換
            Point formPoint = this.pictureBox.PointToScreen(e.Location);
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
                    UpdateCursor(formPoint);
                }
                else
                {
                    // 枠から離れている場合は通常のカーソル
                    this.pictureBox.Cursor = Cursors.Default;
                }
            }
        }

        /// <summary>
        /// PictureBoxのマウスダウン（サイズ変更開始）
        /// </summary>
        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point formPoint = this.pictureBox.PointToScreen(e.Location);
                formPoint = this.PointToClient(formPoint);

                resizeDirection = GetResizeDirection(formPoint);

                // 枠の近くでのみサイズ変更を開始
                if (resizeDirection != ResizeDirection.None)
                {
                    resizing = true;
                    resizeStart = formPoint;
                    resizeStartSize = this.Size;
                    resizeStartLocation = this.Location;
                    this.Capture = true;
                }
            }
        }

        /// <summary>
        /// PictureBoxのマウスアップ（サイズ変更終了）
        /// </summary>
        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (resizing)
            {
                resizing = false;
                resizeDirection = ResizeDirection.None;
                this.Capture = false;

                // サイズ変更完了時に保存
                SaveCurrentNoteState();
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
            if (MessageBox.Show(AppConstants.StickyNoteFormMsg.MSG_CONFIRM_DELETE, AppConstants.SharedTitle.CONFIRM, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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


        /// <summary>
        /// 画像を挿入・編集（画像を再選択して貼り換える）
        /// </summary>
        private void InsertEditImageMenuItem_Click(object sender, EventArgs e)
        {
            // ImageManagerの既存メソッドを呼び出すだけでOK
            // このメソッドは内部で「古い画像を削除→新しい画像を保存」を自動実行
            imageManager?.SelectAndLoadImageFromFile();
        }

        /// <summary>
        /// 画像を削除
        /// </summary>
        private void RemoveImageMenuItem_Click(object sender, EventArgs e)
        {
            if (pictureBox?.Image != null)
            {
                // 確認メッセージを表示
                var result = MessageBox.Show(
                    AppConstants.StickyNoteFormMsg.MSG_CONFIRM_REMOVE_IMAGE,
                    AppConstants.SharedTitle.CONFIRM,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    // ImageManagerの削除メソッドを呼び出し
                    imageManager?.RemoveImage();
                }
            }
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
            reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, AppConstants.StickyNoteFormConfig.REMINDER_TIME_10MIN);
        }

        /// <summary>
        /// リマインダーメニュー:30分後
        /// </summary>
        private void Reminder30MinMenuItem_Click(object sender, EventArgs e)
        {
            reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, AppConstants.StickyNoteFormConfig.REMINDER_TIME_30MIN);
        }

        /// <summary>
        /// リマインダーメニュー:60分後
        /// </summary>
        private void Reminder60MinMenuItem_Click(object sender, EventArgs e)
        {
            reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, AppConstants.StickyNoteFormConfig.REMINDER_TIME_60MIN);
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
    }
}