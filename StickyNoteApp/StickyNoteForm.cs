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

        // サイズ変更用の変数
        private bool resizing = false;
        private Point resizeStart;
        private Size resizeStartSize;
        private Point resizeStartLocation;
        private ResizeDirection resizeDirection = ResizeDirection.None;
        private const int RESIZE_BORDER_WIDTH = 8; // リサイズ可能な枠の幅
        private const int MIN_WIDTH = 150; // 最小幅
        private const int MIN_HEIGHT = 100; // 最小高さ

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

        public StickyNoteForm()
        {
            InitializeComponent(); // フォームデザイナーで設定したUI要素の初期化
            InitializeAutoSave(); // 自動保存タイマーの初期化
            InitializeResizeHandlers(); // サイズ変更ハンドラーの初期化
            InitializeReminder(); // リマインダー管理の初期化

            // 最前面表示の初期状態を反映
            UpdateTopMostMenuState();

            System.Diagnostics.Debug.WriteLine($"付箋作成: ID={NoteId}");
        }

        /// <summary>
        /// サイズ変更ハンドラーの初期化
        /// </summary>
        private void InitializeResizeHandlers()
        {
            // フォーム全体のマウスイベント
            this.MouseMove += Form_MouseMove;
            this.MouseDown += Form_MouseDown;
            this.MouseUp += Form_MouseUp;

            // テキストボックスのマウスイベントも処理（Dock=Fillのため）
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
            Point formPoint = this.txtNote.PointToClient(Control.MousePosition);
            formPoint.Y += this.titleBar.Height; // タイトルバーの高さを加算

            if (resizing)
            {
                PerformResize(formPoint);
            }
            else
            {
                UpdateCursorForTextBox(formPoint);
            }
        }

        /// <summary>
        /// テキストボックスのマウスダウン（サイズ変更開始）
        /// </summary>
        private void TxtNote_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point formPoint = this.txtNote.PointToClient(Control.MousePosition);
                formPoint.Y += this.titleBar.Height;

                resizeDirection = GetResizeDirection(formPoint);
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
                needsSave = true;
                this.txtNote.Cursor = Cursors.Default;
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] サイズ変更終了: {this.Width}x{this.Height}");
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
                    this.txtNote.Cursor = Cursors.Default;
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
                needsSave = true;
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] サイズ変更終了: {this.Width}x{this.Height}");
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
            this.txtNote.TextChanged += StickyNoteContentChanged;
            this.LocationChanged += OnLocationChanged;
            this.SizeChanged += OnSizeChanged;
            this.BackColorChanged += StickyNoteContentChanged;
        }

        /// <summary>
        /// リマインダーの初期化
        /// </summary>
        private void InitializeReminder()
        {
            reminderManager = new ReminderManager();
            reminderManager.ReminderTriggered += (s, e) =>
            {
                this.TopMost = true;
                this.Activate();
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] リマインダー通知表示");
            };
        }

        /// <summary>
        /// 位置変更時の処理
        /// </summary>
        private void OnLocationChanged(object sender, EventArgs e)
        {
            // ドラッグ中またはサイズ変更中は保存フラグを立てない
            // （ドラッグ終了時、サイズ変更終了時に立てる
            if (!dragging && !resizing)
            {
                needsSave = true;
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] 位置変更検出");
            }
        }

        /// <summary>
        /// サイズ変更時の処理
        /// </summary>
        private void OnSizeChanged(object sender, EventArgs e)
        {
            // サイズ変更中は保存フラグを立てない
            // （サイズ変更終了時に立てる
            if (!resizing)
            {
                needsSave = true;
                System.Diagnostics.Debug.WriteLine($"[{NoteId}] サイズ変更検出");
            }
        }

        /// <summary>
        /// 内容変更時の処理(付箋の表示内容や状態が変更された時)
        /// </summary>
        private void StickyNoteContentChanged(object sender, EventArgs e)
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
        private void newNote_Click(object sender, EventArgs e)
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
            this.BackColorChanged -= StickyNoteContentChanged;

            // テキストボックスの色のみを変更
            this.txtNote.BackColor = color;
            // データベース保存用にフォームのBackColorも更新
            this.BackColor = color;
            // タイトルバーの色を確実に固定（念のため再設定）
            this.titleBar.BackColor = Color.WhiteSmoke;

            // イベントを再登録
            this.BackColorChanged += StickyNoteContentChanged;

            needsSave = true;

            System.Diagnostics.Debug.WriteLine($"[{NoteId}] 色変更: {color.Name}");
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
        /// リマインダーメニュー：カスタム時間指定
        /// </summary>
        private void reminderCustomMenuItem_Click(object sender, EventArgs e)
        {
            // カスタム時間入力ダイアログを表示
            using (var inputForm = new Form())
            {
                inputForm.Text = "リマインダー時間設定";
                inputForm.Width = 300;
                inputForm.Height = 150;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                var label = new Label()
                {
                    Text = "通知までの時間を入力してください（1〜60分）:",
                    Left = 20,
                    Top = 20,
                    Width = 250
                };

                var numericUpDown = new NumericUpDown()
                {
                    Left = 20,
                    Top = 50,
                    Width = 100,
                    Minimum = 1,
                    Maximum = 60,
                    Value = 5
                };

                var labelMin = new Label()
                {
                    Text = "分後",
                    Left = 125,
                    Top = 53,
                    Width = 40
                };

                var okButton = new Button()
                {
                    Text = "設定",
                    Left = 100,
                    Top = 80,
                    Width = 80,
                    DialogResult = DialogResult.OK
                };

                var cancelButton = new Button()
                {
                    Text = "キャンセル",
                    Left = 190,
                    Top = 80,
                    Width = 80,
                    DialogResult = DialogResult.Cancel
                };

                inputForm.Controls.Add(label);
                inputForm.Controls.Add(numericUpDown);
                inputForm.Controls.Add(labelMin);
                inputForm.Controls.Add(okButton);
                inputForm.Controls.Add(cancelButton);
                inputForm.AcceptButton = okButton;
                inputForm.CancelButton = cancelButton;

                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    int minutes = (int)numericUpDown.Value;
                    SetReminder(minutes);
                }
            }
        }

        /// <summary>
        /// リマインダーメニュー：10分後
        /// </summary>
        private void reminder10MinMenuItem_Click(object sender, EventArgs e)
        {
            SetReminder(10);
        }

        /// <summary>
        /// リマインダーメニュー：30分後
        /// </summary>
        private void reminder30MinMenuItem_Click(object sender, EventArgs e)
        {
            SetReminder(30);
        }

        /// <summary>
        /// リマインダーメニュー：60分後
        /// </summary>
        private void reminder60MinMenuItem_Click(object sender, EventArgs e)
        {
            SetReminder(60);
        }

        /// <summary>
        /// リマインダーメニュー：キャンセル
        /// </summary>
        private void reminderCancelMenuItem_Click(object sender, EventArgs e)
        {
            if (reminderManager != null && reminderManager.IsActive)
            {
                reminderManager.CancelReminder();
                MessageBox.Show("リマインダーをキャンセルしました。", "リマインダー", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("設定されているリマインダーはありません。", "リマインダー", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// リマインダーを設定
        /// </summary>
        private void SetReminder(int minutes)
        {
            if (reminderManager == null)
            {
                MessageBox.Show("リマインダーマネージャーが初期化されていません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            reminderManager.SetReminder(NoteId, txtNote.Text, minutes);

            DateTime reminderTime = DateTime.Now.AddMinutes(minutes);
            MessageBox.Show(
                $"{minutes}分後にリマインダーを通知します。\n\n通知時刻: {reminderTime:HH:mm:ss}",
                "リマインダー設定",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        /// <summary>
        /// テキストを設定（復元時用）
        /// </summary>
        public void SetText(string text)
        {
            // イベントを一時的に解除
            this.txtNote.TextChanged -= StickyNoteContentChanged;
            txtNote.Text = text;
            // イベントを再登録
            this.txtNote.TextChanged += StickyNoteContentChanged;

            System.Diagnostics.Debug.WriteLine($"[{NoteId}] テキスト設定: {text}");
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
        /// 付箋データを保存
        /// </summary>
        private void SaveNote()
        {
            try
            {
                Database.SaveOrUpdate(this);

                string preview = string.IsNullOrEmpty(txtNote.Text) ? "(空)" :
                    (txtNote.Text.Length > 20 ? txtNote.Text.Substring(0, 20) + "..." : txtNote.Text);

                System.Diagnostics.Debug.WriteLine($"✓ 保存成功 [{NoteId}]: '{preview}' at ({Left},{Top}) size ({Width}x{Height})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ 保存エラー [{NoteId}]: {ex.Message}");
                MessageBox.Show($"保存エラー:\n{ex.Message}", "エラー");
            }
        }

        /// <summary>
        /// フォームクローズ時の処理
        /// フォームが閉じられる直前に自動的に呼び出されます（×ボタン、Alt+F4、Close()メソッドなど）。
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

            if (reminderManager != null)
            {
                reminderManager.Dispose();
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