using System;
using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// 画面キャプチャ用オーバーレイフォーム
    /// </summary>
    public class ScreenCaptureOverlay : Form
    {
        private Point startPoint;
        private Point endPoint;
        private bool isSelecting = false;
        private Bitmap screenCapture;

        public event EventHandler<CaptureCompletedEventArgs> CaptureCompleted;

        public ScreenCaptureOverlay()
        {
            InitializeOverlay();
            RegisterEvents();

            System.Diagnostics.Debug.WriteLine("画面キャプチャオーバーレイ初期化完了");
        }

        /// <summary>
        /// オーバーレイの初期設定
        /// </summary>
        private void InitializeOverlay()
        {
            // フォーム設定
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.Cursor = Cursors.Cross;
            this.BackColor = Color.Black;
            this.Opacity = 0.3; // 半透明
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.PrimaryScreen.Bounds;
        }

        /// <summary>
        /// イベント登録
        /// </summary>
        private void RegisterEvents()
        {
            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;
            this.KeyDown += OnKeyDown;
            this.Paint += OnPaint;
            this.Load += OnLoad;
        }

        /// <summary>
        /// フォームロード時に画面をキャプチャ
        /// </summary>
        private void OnLoad(object sender, EventArgs e)
        {
            CaptureScreen();
        }

        /// <summary>
        /// 画面全体をキャプチャ
        /// </summary>
        private void CaptureScreen()
        {
            try
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                screenCapture = new Bitmap(bounds.Width, bounds.Height);

                using (Graphics g = Graphics.FromImage(screenCapture))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }

                System.Diagnostics.Debug.WriteLine($"画面キャプチャ完了: {bounds.Width}x{bounds.Height}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"画面キャプチャエラー: {ex.Message}");
                MessageBox.Show($"画面キャプチャに失敗しました:\n{ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        /// <summary>
        /// マウスダウン時の処理
        /// </summary>
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                startPoint = e.Location;
                endPoint = e.Location;
                System.Diagnostics.Debug.WriteLine($"選択開始: {startPoint}");
            }
        }

        /// <summary>
        /// マウス移動時の処理
        /// </summary>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                endPoint = e.Location;
                this.Invalidate(); // 再描画
            }
        }

        /// <summary>
        /// マウスアップ時の処理
        /// </summary>
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isSelecting)
            {
                isSelecting = false;

                // 選択範囲を取得
                Rectangle selectedArea = GetSelectedRectangle();

                System.Diagnostics.Debug.WriteLine($"選択完了: {selectedArea}");

                // 最小サイズチェック
                if (selectedArea.Width > 5 && selectedArea.Height > 5)
                {
                    try
                    {
                        // 選択範囲をキャプチャ
                        Bitmap capturedImage = CaptureSelectedArea(selectedArea);

                        // イベント発火
                        CaptureCompleted?.Invoke(this, new CaptureCompletedEventArgs(capturedImage));

                        System.Diagnostics.Debug.WriteLine("キャプチャ完了イベント発火");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"キャプチャエラー: {ex.Message}");
                        MessageBox.Show($"キャプチャに失敗しました:\n{ex.Message}", "エラー",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("選択範囲が小さすぎます");
                }

                this.Close();
            }
        }

        /// <summary>
        /// キーダウン時の処理
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // ESCキーでキャンセル
            if (e.KeyCode == Keys.Escape)
            {
                System.Diagnostics.Debug.WriteLine("キャプチャキャンセル（ESC）");
                this.Close();
            }
        }

        /// <summary>
        /// 描画処理
        /// </summary>
        private void OnPaint(object sender, PaintEventArgs e)
        {
            if (isSelecting)
            {
                Rectangle rect = GetSelectedRectangle();

                // 選択範囲の枠を描画
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }

                // 選択範囲の四隅に小さな四角を描画
                DrawCornerMarkers(e.Graphics, rect);

                // 選択範囲のサイズを表示
                DrawSizeText(e.Graphics, rect);
            }
        }

        /// <summary>
        /// 四隅のマーカーを描画
        /// </summary>
        private void DrawCornerMarkers(Graphics g, Rectangle rect)
        {
            int markerSize = 8;
            using (SolidBrush brush = new SolidBrush(Color.Red))
            {
                // 左上
                g.FillRectangle(brush, rect.Left - markerSize / 2, rect.Top - markerSize / 2, markerSize, markerSize);
                // 右上
                g.FillRectangle(brush, rect.Right - markerSize / 2, rect.Top - markerSize / 2, markerSize, markerSize);
                // 左下
                g.FillRectangle(brush, rect.Left - markerSize / 2, rect.Bottom - markerSize / 2, markerSize, markerSize);
                // 右下
                g.FillRectangle(brush, rect.Right - markerSize / 2, rect.Bottom - markerSize / 2, markerSize, markerSize);
            }
        }

        /// <summary>
        /// サイズテキストを描画
        /// </summary>
        private void DrawSizeText(Graphics g, Rectangle rect)
        {
            string sizeText = $"{rect.Width} × {rect.Height}";

            using (Font font = new Font("Arial", 12, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            {
                // テキストのサイズを測定
                SizeF textSize = g.MeasureString(sizeText, font);

                // 背景の矩形
                RectangleF bgRect = new RectangleF(
                    rect.X,
                    rect.Y - textSize.Height - 5,
                    textSize.Width + 10,
                    textSize.Height + 4
                );

                // 背景を描画
                g.FillRectangle(bgBrush, bgRect);

                // テキストを描画
                g.DrawString(sizeText, font, textBrush, rect.X + 5, rect.Y - textSize.Height - 3);
            }
        }

        /// <summary>
        /// 選択範囲の矩形を取得
        /// </summary>
        private Rectangle GetSelectedRectangle()
        {
            int x = Math.Min(startPoint.X, endPoint.X);
            int y = Math.Min(startPoint.Y, endPoint.Y);
            int width = Math.Abs(endPoint.X - startPoint.X);
            int height = Math.Abs(endPoint.Y - startPoint.Y);

            return new Rectangle(x, y, width, height);
        }

        /// <summary>
        /// 選択範囲をキャプチャ
        /// </summary>
        private Bitmap CaptureSelectedArea(Rectangle area)
        {
            Bitmap capture = new Bitmap(area.Width, area.Height);

            using (Graphics g = Graphics.FromImage(capture))
            {
                // 高品質な描画設定
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                // 選択範囲を切り取り
                g.DrawImage(screenCapture,
                    new Rectangle(0, 0, area.Width, area.Height),
                    area,
                    GraphicsUnit.Pixel);
            }

            return capture;
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (screenCapture != null)
                {
                    screenCapture.Dispose();
                    screenCapture = null;
                }
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// キャプチャ完了イベント引数
    /// </summary>
    public class CaptureCompletedEventArgs : EventArgs
    {
        public Bitmap CapturedImage { get; }

        public CaptureCompletedEventArgs(Bitmap image)
        {
            CapturedImage = image;
        }
    }
}