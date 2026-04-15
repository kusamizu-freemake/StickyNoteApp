using System;
using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// リサイズ＋ドラッグ移動
    /// </summary>
    public partial class StickyNoteForm : Form
    {
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

        // -------------------------
        // フォーム本体のマウスイベント
        // -------------------------

        /// <summary>
        /// マウス移動時の処理（カーソル変更とサイズ変更）
        /// </summary>
        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (resizing)
            {
                PerformResize(e.Location);
            }
            else
            {
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

        // -------------------------
        // テキストボックスのマウスイベント
        // -------------------------

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

        // -------------------------
        // PictureBox のマウスイベント（リサイズ部分）
        // ※ PictureBox の初期化・UI は StickyNoteForm.Image.cs で行う
        // -------------------------

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

        // -------------------------
        // タイトルバーのドラッグ移動
        // -------------------------

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

        // -------------------------
        // リサイズ補助メソッド
        // -------------------------

        /// <summary>
        /// マウス位置からサイズ変更の方向を判定
        /// </summary>
        private ResizeDirection GetResizeDirection(Point location)
        {
            int formWidth = this.ClientSize.Width;
            int formHeight = this.ClientSize.Height;

            bool isLeft   = location.X <= AppConstants.StickyNoteFormConfig.RESIZE_BORDER_WIDTH;
            bool isRight  = location.X >= formWidth  - AppConstants.StickyNoteFormConfig.RESIZE_BORDER_WIDTH;
            bool isTop    = location.Y <= AppConstants.StickyNoteFormConfig.RESIZE_BORDER_WIDTH;
            bool isBottom = location.Y >= formHeight - AppConstants.StickyNoteFormConfig.RESIZE_BORDER_WIDTH;

            // 角の判定（優先度高）
            if (isTop    && isLeft)  return ResizeDirection.TopLeft;
            if (isTop    && isRight) return ResizeDirection.TopRight;
            if (isBottom && isLeft)  return ResizeDirection.BottomLeft;
            if (isBottom && isRight) return ResizeDirection.BottomRight;

            // 辺の判定
            if (isLeft)   return ResizeDirection.Left;
            if (isRight)  return ResizeDirection.Right;
            if (isTop)    return ResizeDirection.Top;
            if (isBottom) return ResizeDirection.Bottom;

            return ResizeDirection.None;
        }

        /// <summary>
        /// カーソルの形状を更新（フォーム・PictureBox 用）
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
        /// サイズ変更を実行
        /// </summary>
        private void PerformResize(Point currentLocation)
        {
            int deltaX = currentLocation.X - resizeStart.X;
            int deltaY = currentLocation.Y - resizeStart.Y;

            int newWidth    = resizeStartSize.Width;
            int newHeight   = resizeStartSize.Height;
            int newLeft     = resizeStartLocation.X;
            int newTop      = resizeStartLocation.Y;

            switch (resizeDirection)
            {
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
                    newWidth  = Math.Max(AppConstants.StickyNoteFormConfig.MIN_WIDTH,  resizeStartSize.Width  + deltaX);
                    newHeight = Math.Max(AppConstants.StickyNoteFormConfig.MIN_HEIGHT, resizeStartSize.Height + deltaY);
                    break;

                // 左下方向にリサイズ
                case ResizeDirection.BottomLeft:
                    newWidth  = Math.Max(AppConstants.StickyNoteFormConfig.MIN_WIDTH,  resizeStartSize.Width  - deltaX);
                    newHeight = Math.Max(AppConstants.StickyNoteFormConfig.MIN_HEIGHT, resizeStartSize.Height + deltaY);
                    if (newWidth > AppConstants.StickyNoteFormConfig.MIN_WIDTH)
                    {
                        newLeft = resizeStartLocation.X + deltaX;
                    }
                    break;

                // 右上方向にリサイズ
                case ResizeDirection.TopRight:
                    newWidth  = Math.Max(AppConstants.StickyNoteFormConfig.MIN_WIDTH,  resizeStartSize.Width  + deltaX);
                    newHeight = Math.Max(AppConstants.StickyNoteFormConfig.MIN_HEIGHT, resizeStartSize.Height - deltaY);
                    if (newHeight > AppConstants.StickyNoteFormConfig.MIN_HEIGHT)
                    {
                        newTop = resizeStartLocation.Y + deltaY;
                    }
                    break;

                // 左上方向にリサイズ
                case ResizeDirection.TopLeft:
                    newWidth  = Math.Max(AppConstants.StickyNoteFormConfig.MIN_WIDTH,  resizeStartSize.Width  - deltaX);
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
    }
}
