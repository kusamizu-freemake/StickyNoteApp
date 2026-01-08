using System;
using System.Diagnostics;
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
        private const int DEFAULT_IMAGE_HEIGHT = 150; // デフォルト画像表示高さ
        private const int IMAGE_LEFT_MARGIN = 0; // 画像の左マージン
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

        
        // 復元中フラグ
        private bool isRestoring = false;

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

        // 画面キャプチャ用
        private PictureBox pictureBox;
        private string capturedImagePath;

        /// <summary>
        /// キャプチャ画像のパス（データベース保存用）
        /// </summary>
        public string CapturedImagePath => capturedImagePath;

        public StickyNoteForm()
        {
            InitializeComponent();            // フォームデザイナーで設定したUI要素の初期化
            InitializeEventHandlers();        // 付箋内容変更検知用のイベントハンドラーの初期化
            InitializeResizeHandlers();       // サイズ変更ハンドラーの初期化（リサイズ機能）
            InitializeReminder();             // リマインダー管理の初期化
            InitializePictureBox();           // 画面キャプチャ用のPictureBox初期化

            // 最前面表示の初期状態を反映
            UpdateTopMostMenuState();

            System.Diagnostics.Debug.WriteLine($"付箋作成: ID={NoteId}");
        }


        /// <summary>
        /// 付箋内容変更検知用のイベントハンドラーの初期化
        /// </summary>
        private void InitializeEventHandlers()
        {
            // テキストボックスからフォーカスが外れた時に保存へ変更
            this.txtNote.Leave += TxtNote_Leave;

            // 位置・サイズ変更は操作完了時に保存（ドラッグ/リサイズ終了時）
            // LocationChanged/SizeChangedイベントは登録しない

            // 背景色変更時：色変更操作完了時に保存
            this.BackColorChanged += BackColor_Changed;
        }
        /// <summary>
        /// テキストボックスからフォーカスが外れた時の処理
        /// </summary>
        private void TxtNote_Leave(object sender, EventArgs e)
        {
            // 復元中は保存しない
            if (isRestoring) return;

            SaveCurrentNoteState();
            System.Diagnostics.Debug.WriteLine($"[{NoteId}] フォーカス喪失→保存");

            // TopMostが無効な場合、付箋を前面に戻す
            if (!this.TopMost)
            {
                this.BringToFront();
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] フォーカス喪失後に前面表示");
            }
        }


        /// <summary>
        /// 背景色変更時の処理
        /// </summary>
        private void BackColor_Changed(object sender, EventArgs e)
        {
            // 復元中は保存しない
            if (isRestoring) return;

            // 色変更完了時に保存
            SaveCurrentNoteState();
            System.Diagnostics.Debug.WriteLine($"[{NoteId}] 背景色変更→保存");
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

                System.Diagnostics.Debug.WriteLine($"[{NoteId}] サイズ変更終了→保存: {this.Width}x{this.Height}");
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

                System.Diagnostics.Debug.WriteLine($"[{NoteId}] サイズ変更終了→保存: {this.Width}x{this.Height}");
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
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] リマインダー通知表示");
            };
        }

        /// <summary>
        /// 現在の付箋状態を保存
        /// ReminderManagerから呼ばれる保存メソッド
        /// </summary>
        public void SaveCurrentNoteState()
        {
            try
            {
                // 現在の付箋情報をすべてDatabase.SaveOrUpdate()に渡す
                Database.SaveOrUpdate(this);

                string preview = string.IsNullOrEmpty(txtNote.Text) ? "(空)" :
                    (txtNote.Text.Length > PREVIEW_TEXT_MAX_LENGTH ?
                     txtNote.Text.Substring(0, PREVIEW_TEXT_MAX_LENGTH) + "..." :
                     txtNote.Text);

                System.Diagnostics.Debug.WriteLine($"✓ 保存成功 [{NoteId}]: '{preview}' at ({Left},{Top}) size ({Width}x{Height})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ 保存エラー [{NoteId}]: {ex.Message}");
                // エラーメッセージは表示しない（連続保存でダイアログが出続けるのを防ぐ）
                //MessageBox.Show($"保存エラー:\n{ex.Message}", "エラー");
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
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] リマインダー復元完了: {reminderTime:yyyy-MM-dd HH:mm:ss}");
            }
        }

        /// <summary>
        /// PictureBoxの初期化 製作中
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
            deleteImageItem.Click += (s, e) => RemoveCapturedImage();
            imageContextMenu.Items.Add(deleteImageItem);

            var replaceImageItem = new ToolStripMenuItem("画像を置き換え");
            replaceImageItem.Click += (s, e) => captureMenuItem_Click(s, e);
            imageContextMenu.Items.Add(replaceImageItem);

            imageContextMenu.Items.Add(new ToolStripSeparator());

            var copyImageItem = new ToolStripMenuItem("画像をコピー");
            copyImageItem.Click += CopyImage_Click;
            imageContextMenu.Items.Add(copyImageItem);

            var saveImageAsItem = new ToolStripMenuItem("画像を名前を付けて保存");
            saveImageAsItem.Click += SaveImageAs_Click;
            imageContextMenu.Items.Add(saveImageAsItem);

            imageContextMenu.Items.Add(new ToolStripSeparator());

            var resizeImageItem = new ToolStripMenuItem("画像サイズ");

            var sizeSmallItem = new ToolStripMenuItem("小 (100px)");
            sizeSmallItem.Click += (s, e) => ResizeImage(IMAGE_SIZE_SMALL);
            resizeImageItem.DropDownItems.Add(sizeSmallItem);

            var sizeMediumItem = new ToolStripMenuItem("中 (150px)");
            sizeMediumItem.Click += (s, e) => ResizeImage(IMAGE_SIZE_MEDIUM);
            resizeImageItem.DropDownItems.Add(sizeMediumItem);

            var sizeLargeItem = new ToolStripMenuItem("大 (200px)");
            sizeLargeItem.Click += (s, e) => ResizeImage(IMAGE_SIZE_LARGE);
            resizeImageItem.DropDownItems.Add(sizeLargeItem);

            var sizeExtraLargeItem = new ToolStripMenuItem("特大 (250px)");
            sizeExtraLargeItem.Click += (s, e) => ResizeImage(IMAGE_SIZE_EXTRA_LARGE);
            resizeImageItem.DropDownItems.Add(sizeExtraLargeItem);

            imageContextMenu.Items.Add(resizeImageItem);

            pictureBox.ContextMenuStrip = imageContextMenu;

            // txtNoteの前に追加（タイトルバーの下）
            this.Controls.Add(pictureBox);
            // pictureBox を txtNote より前（上）に移動
            this.Controls.SetChildIndex(pictureBox, 0);
        }

        /// <summary>
        /// 画像をコピー
        /// </summary>
        private void CopyImage_Click(object sender, EventArgs e)
        {
            if (pictureBox.Image != null)
            {
                try
                {
                    Clipboard.SetImage(pictureBox.Image);
                    System.Diagnostics.Debug.WriteLine($"[{NoteId}] 画像をクリップボードにコピー");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"画像のコピーに失敗しました:\n{ex.Message}", "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 画像を名前を付けて保存
        /// </summary>
        private void SaveImageAs_Click(object sender, EventArgs e)
        {
            if (pictureBox.Image != null)
            {
                try
                {
                    using (var saveDialog = new SaveFileDialog())
                    {
                        saveDialog.Filter = "PNG画像|*.png|JPEG画像|*.jpg|BMP画像|*.bmp|すべてのファイル|*.*";
                        saveDialog.DefaultExt = "png";
                        saveDialog.FileName = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.png";

                        if (saveDialog.ShowDialog() == DialogResult.OK)
                        {
                            var format = System.Drawing.Imaging.ImageFormat.Png;
                            string ext = System.IO.Path.GetExtension(saveDialog.FileName).ToLower();
                            switch (ext)
                            {
                                case ".jpg":
                                case ".jpeg":
                                    format = System.Drawing.Imaging.ImageFormat.Jpeg;
                                    break;
                                case ".bmp":
                                    format = System.Drawing.Imaging.ImageFormat.Bmp;
                                    break;
                            }

                            pictureBox.Image.Save(saveDialog.FileName, format);
                            MessageBox.Show($"画像を保存しました:\n{saveDialog.FileName}", "保存完了",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"画像の保存に失敗しました:\n{ex.Message}", "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 画像サイズを変更
        /// </summary>
        private void ResizeImage(int height)
        {
            if (pictureBox.Visible)
            {
                pictureBox.Height = height;
                // テキストボックスの位置を調整
                txtNote.Top = pictureBox.Bottom;
                txtNote.Height = this.ClientSize.Height - txtNote.Top;

                // 画像サイズ変更完了時に保存
                SaveCurrentNoteState();

                System.Diagnostics.Debug.WriteLine($"[{NoteId}] 画像サイズ変更→保存: {height}px");
            }
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
                RemoveCapturedImage();
            }
        }

        /// <summary>
        /// キャプチャ画像を削除
        /// </summary>
        private void RemoveCapturedImage()
        {
            if (pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
                pictureBox.Image = null;
            }

            pictureBox.Height = 0;
            pictureBox.Visible = false;

            // 画像ファイルを削除
            if (!string.IsNullOrEmpty(capturedImagePath) && System.IO.File.Exists(capturedImagePath))
            {
                try
                {
                    System.IO.File.Delete(capturedImagePath);
                    System.Diagnostics.Debug.WriteLine($"[{NoteId}] 画像ファイル削除: {capturedImagePath}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[{NoteId}] 画像ファイル削除エラー: {ex.Message}");
                }
            }

            capturedImagePath = null;
            // テキストボックスの位置を調整
            txtNote.Dock = DockStyle.Fill;

            // 画像削除完了時に保存
            SaveCurrentNoteState();

            System.Diagnostics.Debug.WriteLine($"[{NoteId}] 画像削除→保存");
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

                System.Diagnostics.Debug.WriteLine($"[{NoteId}] ドラッグ終了→保存");
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
        private void StickyNoteMenu_New_Click(object sender, EventArgs e)
        {
            StickyNoteForm newNote = new StickyNoteForm();
            // 現在の付箋の近くに表示
            newNote.Location = new Point(this.Left + NEW_NOTE_OFFSET_X, this.Top + NEW_NOTE_OFFSET_Y);
            newNote.Show();
            System.Diagnostics.Debug.WriteLine($"右クリックから新しい付箋作成: ID={newNote.NoteId}");
        }

        /// <summary>
        /// 右クリックメニュー：最前面表示の切り替え
        /// </summary>
        private void topMostMenuItem_Click(object sender, EventArgs e)
        {
            this.TopMost = topMostMenuItem.Checked;

            // 最前面表示切り替え完了時に保存
            SaveCurrentNoteState();

            System.Diagnostics.Debug.WriteLine($"[{NoteId}] 最前面表示切り替え→保存: {this.TopMost}");
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

            System.Diagnostics.Debug.WriteLine($"[{NoteId}] 色変更→保存: {color.Name}");
        }

        /// <summary>
        /// 色メニュー：イエロー
        /// </summary>
        private void colorYellowMenuItem_Click(object sender, EventArgs e)
        {
            ChangeColor(Color.Khaki);
        }

        /// <summary>
        /// 色メニュー：ピンク
        /// </summary>
        private void colorPinkMenuItem_Click(object sender, EventArgs e)
        {
            ChangeColor(Color.LightPink);
        }

        /// <summary>
        /// 色メニュー：ブルー
        /// </summary>
        private void colorBlueMenuItem_Click(object sender, EventArgs e)
        {
            ChangeColor(Color.LightBlue);
        }

        /// <summary>
        /// 色メニュー：グリーン
        /// </summary>
        private void colorGreenMenuItem_Click(object sender, EventArgs e)
        {
            ChangeColor(Color.LightGreen);
        }

        /// <summary>
        /// 色メニュー：オレンジ
        /// </summary>
        private void colorOrangeMenuItem_Click(object sender, EventArgs e)
        {
            ChangeColor(Color.LightSalmon);
        }

        /// <summary>
        /// 色メニュー：パープル
        /// </summary>
        private void colorPurpleMenuItem_Click(object sender, EventArgs e)
        {
            ChangeColor(Color.Plum);
        }

        /// <summary>
        /// 画面キャプチャメニュー
        /// </summary>
        private void captureMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[{NoteId}] 画面キャプチャ開始");

            try
            {
                // オーバーレイフォームを表示
                var overlay = new ScreenCaptureOverlay();
                overlay.CaptureCompleted += Overlay_CaptureCompleted;
                overlay.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] キャプチャエラー: {ex.Message}");
                MessageBox.Show($"画面キャプチャに失敗しました:\n{ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// クリップボードから画像を貼り付け
        /// </summary>
        private void pasteImageMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[{NoteId}] クリップボードから画像を貼り付け");

            try
            {
                if (Clipboard.ContainsImage())
                {
                    Image clipboardImage = Clipboard.GetImage();

                    if (clipboardImage != null)
                    {
                        // 既存の画像を削除
                        if (pictureBox.Image != null)
                        {
                            pictureBox.Image.Dispose();
                        }

                        // Bitmapに変換
                        Bitmap bitmap = new Bitmap(clipboardImage);
                        // 画像を保存
                        string imagePath = SaveCapturedImage(bitmap);

                        // PictureBoxに表示
                        pictureBox.Image = bitmap;
                        pictureBox.Height = DEFAULT_IMAGE_HEIGHT;
                        pictureBox.Visible = true;

                        // テキストボックスの位置を調整
                        txtNote.Dock = DockStyle.None;
                        txtNote.Top = pictureBox.Bottom;
                        txtNote.Left = IMAGE_LEFT_MARGIN;
                        txtNote.Width = this.ClientSize.Width;
                        txtNote.Height = this.ClientSize.Height - txtNote.Top;

                        capturedImagePath = imagePath;

                        // 画像貼り付け完了時に保存
                        SaveCurrentNoteState();

                        System.Diagnostics.Debug.WriteLine($"[{NoteId}] クリップボード画像貼り付け→保存: {imagePath}");
                    }
                }
                else
                {
                    MessageBox.Show("クリップボードに画像がありません。", "情報",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] 貼り付けエラー: {ex.Message}");
                MessageBox.Show($"画像の貼り付けに失敗しました:\n{ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// キャプチャ完了時の処理
        /// </summary>
        private void Overlay_CaptureCompleted(object sender, CaptureCompletedEventArgs e)
        {
            try
            {
                // 画像を保存
                string imagePath = SaveCapturedImage(e.CapturedImage);

                // PictureBoxに表示
                if (pictureBox.Image != null)
                {
                    pictureBox.Image.Dispose();
                }

                pictureBox.Image = e.CapturedImage;
                pictureBox.Height = DEFAULT_IMAGE_HEIGHT;
                pictureBox.Visible = true;

                // テキストボックスのDockを解除して位置を調整
                txtNote.Dock = DockStyle.None;
                txtNote.Top = pictureBox.Bottom;
                txtNote.Left = IMAGE_LEFT_MARGIN;
                txtNote.Width = this.ClientSize.Width;
                txtNote.Height = this.ClientSize.Height - txtNote.Top;

                capturedImagePath = imagePath;

                // 画像キャプチャ完了時に保存
                SaveCurrentNoteState();

                System.Diagnostics.Debug.WriteLine($"[{NoteId}] 画像キャプチャ→保存: {imagePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] 画像保存エラー: {ex.Message}");
                MessageBox.Show($"画像の保存に失敗しました:\n{ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// キャプチャ画像をファイルに保存
        /// </summary>
        private string SaveCapturedImage(Bitmap image)
        {
            string folder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StickyNoteApp",
                "Images"
            );

            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }

            string fileName = $"{NoteId}_{DateTime.Now:yyyyMMddHHmmss}.png";
            string filePath = System.IO.Path.Combine(folder, fileName);

            image.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

            System.Diagnostics.Debug.WriteLine($"[{NoteId}] 画像保存: {filePath}");

            return filePath;
        }

        /// <summary>
        /// キャプチャ画像を復元（データベースから読み込み時用）
        /// </summary>
        public void LoadCapturedImage(string imagePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(imagePath) && System.IO.File.Exists(imagePath))
                {
                    pictureBox.Image = Image.FromFile(imagePath);
                    pictureBox.Height = DEFAULT_IMAGE_HEIGHT;
                    pictureBox.Visible = true;

                    // テキストボックスの位置を調整
                    txtNote.Dock = DockStyle.None;
                    txtNote.Top = pictureBox.Bottom;
                    txtNote.Left = IMAGE_LEFT_MARGIN;
                    txtNote.Width = this.ClientSize.Width;
                    txtNote.Height = this.ClientSize.Height - txtNote.Top;

                    capturedImagePath = imagePath;

                    System.Diagnostics.Debug.WriteLine($"[{NoteId}] 画像復元: {imagePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] 画像読み込みエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// リマインダーメニュー:カスタム時間指定
        /// </summary>
        private void reminderCustomMenuItem_Click(object sender, EventArgs e)
        {
            reminderManager?.ShowCustomReminderDialog(NoteId, txtNote.Text);
        }

        /// <summary>
        /// リマインダーメニュー:10分後
        /// </summary>
        private void reminder10MinMenuItem_Click(object sender, EventArgs e)
        {
            reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, REMINDER_TIME_10MIN);
        }

        /// <summary>
        /// リマインダーメニュー:30分後
        /// </summary>
        private void reminder30MinMenuItem_Click(object sender, EventArgs e)
        {
            reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, REMINDER_TIME_30MIN);
        }

        /// <summary>
        /// リマインダーメニュー:60分後
        /// </summary>
        private void reminder60MinMenuItem_Click(object sender, EventArgs e)
        {
            reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, REMINDER_TIME_60MIN);
        }

        /// <summary>
        /// リマインダーメニュー:キャンセル
        /// </summary>
        private void reminderCancelMenuItem_Click(object sender, EventArgs e)
        {
            reminderManager?.ShowCancelReminderDialog();
        }

        /// <summary>
        /// テキストを設定（復元時用）
        /// </summary>
        public void SetText(string text)
        {
            // 復元中フラグを立てる
            isRestoring = true;
            
            txtNote.Text = text;
            System.Diagnostics.Debug.WriteLine($"[{NoteId}] テキスト設定: {text}");

            // 復元中フラグを下ろす
            isRestoring = false;

        }
        // 新規メソッド追加

        /// <summary>
        /// 復元処理の開始を通知
        /// </summary>
        public void BeginRestore()
        {
            isRestoring = true;
        }

        /// <summary>
        /// 復元処理の終了を通知
        /// </summary>
        public void EndRestore()
        {
            isRestoring = false;
            System.Diagnostics.Debug.WriteLine($"[{NoteId}] 復元処理終了");
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

            System.Diagnostics.Debug.WriteLine($"[{NoteId}] フォームクローズ");


            if (reminderManager != null)
            {
                reminderManager.Dispose();
            }

            // 画像のリソースを解放
            if (pictureBox != null && pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
                pictureBox.Image = null;
            }

            // フォームクローズ時に最終保存
            System.Diagnostics.Debug.WriteLine($"[{NoteId}] 最終保存実行");
            SaveCurrentNoteState();
        }
    }
}