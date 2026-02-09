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
        private const long MAX_FILE_SIZE_BYTES = 2 * 1024 * 1024; // 2MB

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
        /// 画像の表示高さを更新する（ImageResizeManagerから呼び出し）
        /// </summary>
        public void SetImageDisplayHeight(int height)
        {
            imageDisplayHeight = height;
        }

        /// <summary>
        /// 画像をリサイズして保存する（実ファイル作成）
        /// </summary>
        /// <param name="height">リサイズ後の高さ（px）</param>
        /// <returns>リサイズ済み画像のパス、失敗時またはアニメーションGIFの場合はnull</returns>
        public string CreateResizedImage(int height)
        {
            if (string.IsNullOrEmpty(originalImagePath))
            {
                System.Diagnostics.Debug.WriteLine("元画像パスが設定されていません");
                return null;
            }

            if (!File.Exists(originalImagePath))
            {
                System.Diagnostics.Debug.WriteLine($"元画像ファイルが見つかりません: {originalImagePath}");
                return null;
            }

            try
            {
                // 古いリサイズ済み画像を削除
                if (!string.IsNullOrEmpty(resizedImagePath))
                {
                    ImageResizer.DeleteResizedImage(resizedImagePath);
                }

                // 新しいサイズでリサイズ実行
                string newResizedPath = ImageResizer.ResizeAndSaveImage(
                    originalImagePath,
                    height,
                    parentForm.NoteId
                );

                // リサイズ済みパスを更新
                resizedImagePath = newResizedPath;

                System.Diagnostics.Debug.WriteLine($"リサイズ済み画像を作成: {newResizedPath}");
                return newResizedPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"画像リサイズエラー: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 現在表示中の画像をユーザーが指定した場所にリサイズして保存する。
        /// </summary>
        /// <param name="height">リサイズ後の高さ（px）、0の場合は元のサイズで保存</param>
        public void SaveResizedImageToUserLocation(int height)
        {
            if (string.IsNullOrEmpty(originalImagePath))
            {
                MessageBox.Show(
                    "保存する画像がありません。",
                    "情報",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (!File.Exists(originalImagePath))
            {
                MessageBox.Show(
                    "元画像ファイルが見つかりません。",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // 推奨ファイル名を生成（付箋IDから）
            string suggestedFileName = $"note_{parentForm.NoteId}_image";

            // height=0の場合は元のサイズで保存
            if (height == 0)
            {
                SaveOriginalImageToUserLocation(suggestedFileName);
            }
            else
            {
                // ユーザー指定場所にリサイズして保存
                ImageResizer.ResizeAndSaveToUserLocation(
                    originalImagePath,
                    height,
                    suggestedFileName
                );
            }
        }

        /// <summary>
        /// 元画像をユーザーが指定した場所にそのまま保存する。
        /// </summary>
        /// <param name="suggestedFileName">推奨ファイル名</param>
        private void SaveOriginalImageToUserLocation(string suggestedFileName)
        {
            try
            {
                using (var saveDialog = new SaveFileDialog())
                {
                    // 元画像の拡張子を取得
                    string originalExt = Path.GetExtension(originalImagePath).ToLower();
                    string defaultExt = string.IsNullOrEmpty(originalExt) ? ".png" : originalExt.TrimStart('.');

                    saveDialog.FileName = $"{suggestedFileName}_original";
                    saveDialog.Filter = "PNG画像|*.png|JPEG画像|*.jpg;*.jpeg|GIF画像|*.gif|すべてのファイル|*.*";

                    // 元画像の拡張子に応じてFilterIndexを設定
                    switch (originalExt)
                    {
                        case ".png":
                            saveDialog.FilterIndex = 1; // PNG
                            break;
                        case ".jpg":
                        case ".jpeg":
                            saveDialog.FilterIndex = 2; // JPEG
                            break;
                        case ".gif":
                            saveDialog.FilterIndex = 3; // GIF
                            break;
                        default:
                            saveDialog.FilterIndex = 1; // デフォルトはPNG
                            break;
                    }

                    saveDialog.Title = "元画像を保存";
                    saveDialog.DefaultExt = defaultExt;

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        // 元画像をコピー
                        File.Copy(originalImagePath, saveDialog.FileName, true);

                        System.Diagnostics.Debug.WriteLine($"元画像を保存: {saveDialog.FileName}");

                        MessageBox.Show(
                            $"元画像を保存しました。\n\n保存先: {saveDialog.FileName}",
                            "保存完了",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"元画像保存エラー: {ex.Message}");
                MessageBox.Show(
                    $"元画像の保存に失敗しました:\n{ex.Message}",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

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
                        // ファイルサイズをチェック
                        FileInfo fileInfo = new FileInfo(openDialog.FileName);
                        if (fileInfo.Length > MAX_FILE_SIZE_BYTES)
                        {
                            double fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                            // サイズ超過の警告(小数点以下2桁まで表示 )
                            MessageBox.Show(
                                $"選択した画像ファイルのサイズが大きすぎます。\n" +
                                $"ファイルサイズ: {fileSizeMB:F2}MB\n" +
                                $"最大サイズ: 2MB\n\n" +
                                $"2MB以下の画像ファイルを選択してください。",
                                "ファイルサイズエラー",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        // 画像を読み込み
                        using (var originalImage = Image.FromFile(openDialog.FileName))
                        {
                            // Bitmapに変換してコピー
                            Bitmap bitmap = new Bitmap(originalImage);

                            // 既存の画像を削除
                            RemoveImageInternal();

                            // 元画像として保存
                            string imagePath = SaveOriginalImage(bitmap, openDialog.FileName);
                            originalImagePath = imagePath;
                            resizedImagePath = null;

                            // PictureBoxに表示
                            LoadImageToPictureBox(imagePath);
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
                // アニメーションを停止
                ImageAnimator.StopAnimate(pictureBox.Image, OnFrameChanged);

                var Image = pictureBox.Image;
                pictureBox.Image = null;
                Image.Dispose();
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
                // アニメーションを停止
                ImageAnimator.StopAnimate(pictureBox.Image, OnFrameChanged);

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
                    // 自動的にアニメーションが開始
                    LoadImageToPictureBox(imageToLoad);
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
        private string SaveOriginalImage(Bitmap image, string sourceFilePath)
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

            // ファイル拡張子を取得（GIFの場合はGIFで保存）
            string sourceExt = Path.GetExtension(sourceFilePath).ToLower();
            string ext = (sourceExt == ".gif") ? ".gif" : ".png";

            string fileName = $"{parentForm.NoteId}_original_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            string filePath = Path.Combine(folder, fileName);

            // GIFの場合は元ファイルをコピー（アニメーション情報を保持）
            if (sourceExt == ".gif")
            {
                File.Copy(sourceFilePath, filePath, true);
            }
            else
            {
                image.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            }

            return filePath;
        }

        /// <summary>
        /// PictureBoxに画像を読み込み、アニメーションGIFに対応
        /// </summary>
        private void LoadImageToPictureBox(string imagePath)
        {
            // 既存の画像のアニメーションを停止
            if (pictureBox.Image != null)
            {
                ImageAnimator.StopAnimate(pictureBox.Image, OnFrameChanged);
                pictureBox.Image.Dispose();
            }

            // 新しい画像を読み込み
            pictureBox.Image = Image.FromFile(imagePath);

            // アニメーションGIFの場合はアニメーションを開始
            if (IsAnimatedGif(pictureBox.Image))
            {
                ImageAnimator.Animate(pictureBox.Image, OnFrameChanged);
            }
        }

        /// <summary>
        /// アニメーションGIFかどうかを判定
        /// </summary>
        private bool IsAnimatedGif(Image image)
        {
            if (image == null)
                return false;

            // GIF形式の番号と一致するか確認
            // 静止画GIFはフレーム数が1, アニメーションGIFはフレーム数が2以上
            if (image.RawFormat.Guid == System.Drawing.Imaging.ImageFormat.Gif.Guid)
            {
                var dimension = new System.Drawing.Imaging.FrameDimension(image.FrameDimensionsList[0]);
                int frameCount = image.GetFrameCount(dimension);
                return frameCount > 1;
            }
            return false;
        }

        /// <summary>
        /// アニメーションフレーム更新時のコールバック
        /// </summary>
        private void OnFrameChanged(object sender, EventArgs e)
        {
            if (pictureBox.InvokeRequired)
            {
                // UIスレッドで実行
                pictureBox.BeginInvoke(new EventHandler(OnFrameChanged), sender, e);
            }
            else
            {
                // フレームを更新して再描画
                ImageAnimator.UpdateFrames(pictureBox.Image);
                //
                pictureBox.Invalidate();
            }
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
        public void AdjustTextBoxPosition()
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
                // アニメーションを停止
                ImageAnimator.StopAnimate(pictureBox.Image, OnFrameChanged);

                var image = pictureBox.Image;
                pictureBox.Image = null;
                image.Dispose();
            }
        }
    }
}