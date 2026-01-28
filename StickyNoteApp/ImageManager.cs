using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// 付箋の画像管理クラス
    /// 画像の読み込み、保存、削除などの機能を提供
    /// </summary>
    public class ImageManager
    {
        // 定数定義
        private const int DEFAULT_IMAGE_HEIGHT = 150;
        private const int IMAGE_LEFT_MARGIN = 0;
        
        private readonly StickyNoteForm parentForm;
        private readonly PictureBox pictureBox;
        
        // 画像パス管理
        private string originalImagePath;      // 元画像のパス
        private string resizedImagePath;       // リサイズ済み画像のパス
        private int imageDisplayHeight = 150;  // 画像表示高さ

        public string OriginalImagePath => originalImagePath;
        public string ResizedImagePath => resizedImagePath;
        public int ImageDisplayHeight => imageDisplayHeight;
        public string CapturedImagePath => resizedImagePath ?? originalImagePath; // 互換性

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ImageManager(StickyNoteForm form, PictureBox picBox)
        {
            parentForm = form;
            pictureBox = picBox;
        }


        /// <summary>
        /// ファイルから画像を選択して読み込み
        /// </summary>
        public void SelectAndLoadImageFromFile()
        {
            try
            {
                using (var openDialog = new OpenFileDialog())
                {
                    openDialog.Filter = "画像ファイル|*.png;*.jpg;*.jpeg;*.gif|" +
                                       "PNGファイル|*.png|" +
                                       "JPEGファイル|*.jpg;*.jpeg|" +
                                       "GIFファイル|*.gif|" +
                                       "すべてのファイル|*.*";
                    openDialog.Title = "画像を選択";
                    openDialog.Multiselect = false;

                    if (openDialog.ShowDialog() == DialogResult.OK)
                    {
                        // 画像を読み込み
                        using (var originalImage = Image.FromFile(openDialog.FileName))
                        {
                            // Bitmapに変換してコピー
                            Bitmap bitmap = new Bitmap(originalImage);

                            // 既存の画像を削除
                            RemoveImageInternal();

                            // 元画像として保存
                            string imagePath = SaveOriginalImage(bitmap);
                            originalImagePath = imagePath;
                            resizedImagePath = null;

                            // PictureBoxに表示
                            pictureBox.Image = bitmap;
                            pictureBox.Height = DEFAULT_IMAGE_HEIGHT;
                            pictureBox.Visible = true;
                            imageDisplayHeight = DEFAULT_IMAGE_HEIGHT;

                            // テキストボックスの位置を調整
                            AdjustTextBoxPosition();

                            // 画像読み込み完了時に保存
                            parentForm.SaveCurrentNoteState();

                            MessageBox.Show(
                                "画像を読み込みました。",
                                "完了",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"画像の読み込みに失敗しました:\n{ex.Message}",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// メッセージボックスを表示せずに画像を削除
        /// </summary>
        private void RemoveImageInternal()
        {
            if (pictureBox.Image != null)
            {
                var Image = pictureBox.Image;
                pictureBox.Image = null;
                Image.Dispose();
            }
        }

        /// <summary>
        /// 画像をコピー
        /// </summary>
        public void CopyImage()
        {
            if (pictureBox.Image != null)
            {
                try
                {
                    Clipboard.SetImage(pictureBox.Image);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"画像のコピーに失敗しました:\n{ex.Message}",
                        "エラー",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 画像を名前を付けて保存
        /// </summary>
        public void SaveImageAs()
        {
            if (pictureBox.Image != null)
            {
                try
                {
                    using (var saveDialog = new SaveFileDialog())
                    {
                        saveDialog.Filter = "PNG画像|*.png|JPEG画像|*.jpg|GIF画像|*.gif|すべてのファイル|*.*";
                        saveDialog.DefaultExt = "png";
                        saveDialog.FileName = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.png";

                        if (saveDialog.ShowDialog() == DialogResult.OK)
                        {
                            var format = System.Drawing.Imaging.ImageFormat.Png;
                            string ext = Path.GetExtension(saveDialog.FileName).ToLower();
                            switch (ext)
                            {
                                case ".jpg":
                                case ".jpeg":
                                    format = System.Drawing.Imaging.ImageFormat.Jpeg;
                                    break;
                                case ".gif":
                                    format = System.Drawing.Imaging.ImageFormat.Gif;
                                    break;
                            }

                            pictureBox.Image.Save(saveDialog.FileName, format);
                            MessageBox.Show(
                                $"画像を保存しました:\n{saveDialog.FileName}",
                                "保存完了",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"画像の保存に失敗しました:\n{ex.Message}",
                        "エラー",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 画像サイズを変更
        /// </summary>
        public void ResizeImage(int height)
        {
            if (pictureBox.Visible)
            {
                pictureBox.Height = height;
                imageDisplayHeight = height;

                // テキストボックスの位置を調整
                AdjustTextBoxPosition();

                // 画像サイズ変更完了時に保存
                parentForm.SaveCurrentNoteState();
            }
        }

        /// <summary>
        /// 画像を削除
        /// </summary>
        public void RemoveImage()
        {
            // 先に画像を解放（順序が重要）
            if (pictureBox.Image != null)
            {
                var image = pictureBox.Image;
                pictureBox.Image = null;
                image.Dispose();
            }

            pictureBox.Height = 0;
            pictureBox.Visible = false;

            // ファイル削除（リトライ機能付き）
            try
            {
                DeleteImageFileWithRetry(originalImagePath);
                DeleteImageFileWithRetry(resizedImagePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"画像ファイル削除時にエラーが発生: {ex.Message}");
            }

            originalImagePath = null;
            resizedImagePath = null;
            imageDisplayHeight = 150;

            // テキストボックスの位置を調整
            parentForm.txtNote.Dock = DockStyle.Fill;

            // 画像削除完了時に保存
            parentForm.SaveCurrentNoteState();
        }

        /// <summary>
        /// 画像を復元（データベースから読み込み時用）
        /// </summary>
        public void LoadImage(string imagePath, string originalPath, string resizedPath, int displayHeight)
        {
            try
            {
                originalImagePath = originalPath;
                resizedImagePath = resizedPath;
                imageDisplayHeight = displayHeight > 0 ? displayHeight : DEFAULT_IMAGE_HEIGHT;

                // 表示する画像を決定
                string imageToLoad = null;
                if (!string.IsNullOrEmpty(resizedPath) && File.Exists(resizedPath))
                {
                    imageToLoad = resizedPath;
                }
                else if (!string.IsNullOrEmpty(originalPath) && File.Exists(originalPath))
                {
                    imageToLoad = originalPath;
                }
                else if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    imageToLoad = imagePath;
                    originalImagePath = imagePath;
                }

                if (!string.IsNullOrEmpty(imageToLoad))
                {
                    pictureBox.Image = Image.FromFile(imageToLoad);
                    pictureBox.Height = imageDisplayHeight;
                    pictureBox.Visible = true;

                    // テキストボックスの位置を調整
                    AdjustTextBoxPosition();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"画像読み込みエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 元画像をファイルとして保存
        /// </summary>
        private string SaveOriginalImage(Bitmap image)
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StickyNoteApp",
                "Images"
            );

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string fileName = $"{parentForm.NoteId}_original_{DateTime.Now:yyyyMMddHHmmss}.png";
            string filePath = Path.Combine(folder, fileName);

            image.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

            return filePath;
        }

        /// <summary>
        /// 画像ファイルをリトライ付きで削除
        /// </summary>
        private void DeleteImageFileWithRetry(string filePath, int maxRetries = 3)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    File.Delete(filePath);
                    System.Diagnostics.Debug.WriteLine($"画像削除成功: {filePath}");
                    return;
                }
                catch (IOException ex)
                {
                    if (i < maxRetries - 1)
                    {
                        System.Diagnostics.Debug.WriteLine($"画像削除リトライ {i + 1}/{maxRetries}: {filePath}");
                        System.Threading.Thread.Sleep(100);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"画像削除失敗（最終リトライ）: {filePath} - {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// テキストボックスの位置を調整
        /// </summary>
        private void AdjustTextBoxPosition()
        {
            parentForm.txtNote.Dock = DockStyle.None;
            parentForm.txtNote.Top = pictureBox.Bottom;
            parentForm.txtNote.Left = IMAGE_LEFT_MARGIN;
            parentForm.txtNote.Width = parentForm.ClientSize.Width;
            parentForm.txtNote.Height = parentForm.ClientSize.Height - parentForm.txtNote.Top;
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        public void Dispose()
        {
            if (pictureBox != null && pictureBox.Image != null)
            {
                var image = pictureBox.Image;
                pictureBox.Image = null;
                image.Dispose();
            }
        }
    }
}