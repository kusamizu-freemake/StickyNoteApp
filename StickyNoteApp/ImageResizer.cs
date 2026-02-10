using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace StickyNoteApp
{
    /// <summary>
    /// 画像ファイルを実際にリサイズして保存するクラス。
    /// アスペクト比を維持してリサイズを行い、指定されたパスに保存する。
    /// </summary>
    public class ImageResizer
    {
        private readonly string imageFolder;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="folder">リサイズ済み画像の保存先フォルダ</param>
        public ImageResizer(string folder)
        {
            imageFolder = folder;

            // フォルダが存在しない場合は作成
            if (!Directory.Exists(imageFolder))
            {
                Directory.CreateDirectory(imageFolder);
            }
        }

        /// <summary>
        /// リサイズ後の画像保存先フォルダを取得
        /// </summary>
        private string GetResizedImageFolder()
        {
            return imageFolder;
        }

        /// <summary>
        /// 画像を指定した高さにリサイズし、アスペクト比を維持する。
        /// </summary>
        /// <param name="originalImage">元画像</param>
        /// <param name="newHeight">リサイズ後の高さ（px）</param>
        /// <returns>リサイズされたBitmap（呼び出し側でDisposeする必要がある）</returns>
        public Bitmap ResizeImage(Image originalImage, int newHeight)
        {
            if (originalImage == null)
                throw new ArgumentNullException(nameof(originalImage));

            if (newHeight <= 0)
                throw new ArgumentException("高さは正の値である必要があります。", nameof(newHeight));

            // アスペクト比を維持して幅を計算
            int newWidth = (int)(originalImage.Width * ((double)newHeight / originalImage.Height));

            // 幅が0にならないようにする
            if (newWidth <= 0) newWidth = 1;

            // リサイズ後の画像を作成
            Bitmap resizedBitmap = new Bitmap(newWidth, newHeight);

            using (Graphics graphics = Graphics.FromImage(resizedBitmap))
            {
                // 画像を描画（リサイズ）
                graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
            }

            return resizedBitmap;
        }

        /// <summary>
        /// 元画像ファイルを読み込み、指定した高さにリサイズして保存する。
        /// </summary>
        /// <param name="originalImagePath">元画像のファイルパス</param>
        /// <param name="newHeight">リサイズ後の高さ（px）</param>
        /// <param name="noteId">付箋ID（ファイル名生成に使用）</param>
        /// <returns>リサイズ後の画像の保存先パス</returns>
        public string ResizeAndSaveImage(string originalImagePath, int newHeight, string noteId)
        {
            if (string.IsNullOrEmpty(originalImagePath))
                throw new ArgumentNullException(nameof(originalImagePath));

            if (!File.Exists(originalImagePath))
                throw new FileNotFoundException("元画像ファイルが見つかりません。", originalImagePath);

            // 元画像の拡張子を取得
            string originalExt = Path.GetExtension(originalImagePath).ToLower();

            // GIFアニメーションの場合はリサイズしない（アニメーション情報が失われるため）
            if (IsAnimatedGif(originalImagePath))
            {
                System.Diagnostics.Debug.WriteLine($"アニメーションGIFはリサイズをスキップします: {originalImagePath}");
                return null; // リサイズしない場合はnullを返す
            }

            try
            {
                // 元画像を読み込み
                using (Image originalImage = Image.FromFile(originalImagePath))
                {
                    // リサイズ実行
                    using (Bitmap resizedBitmap = ResizeImage(originalImage, newHeight))
                    {
                        // 保存先フォルダを確保
                        string folder = GetResizedImageFolder();
                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }

                        // 保存ファイル名を生成（付箋ID_resized_高さpx_タイムスタンプ.拡張子）
                        string ext = (originalExt == ".gif") ? ".gif" : ".png";
                        string fileName = $"{noteId}_resized_{newHeight}px_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                        string savePath = Path.Combine(folder, fileName);

                        // 画像形式を決定
                        ImageFormat format = GetImageFormat(ext);

                        // ファイルに保存
                        resizedBitmap.Save(savePath, format);

                        System.Diagnostics.Debug.WriteLine($"リサイズ画像を保存しました: {savePath}");
                        return savePath;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"画像リサイズエラー: {ex.Message}");
                throw new Exception($"画像のリサイズに失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// アニメーションGIFかどうかを判定する。
        /// </summary>
        private bool IsAnimatedGif(string imagePath)
        {
            try
            {
                using (Image image = Image.FromFile(imagePath))
                {
                    // GIF形式かチェック
                    if (image.RawFormat.Guid != ImageFormat.Gif.Guid)
                        return false;

                    // フレーム数をチェック
                    var dimension = new FrameDimension(image.FrameDimensionsList[0]);
                    int frameCount = image.GetFrameCount(dimension);
                    return frameCount > 1; // フレーム数が2以上ならアニメーションGIF
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 拡張子から適切なImageFormatを取得する。
        /// </summary>
        private ImageFormat GetImageFormat(string extension)
        {
            switch (extension.ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    return ImageFormat.Jpeg;
                case ".gif":
                    return ImageFormat.Gif;
                case ".bmp":
                    return ImageFormat.Bmp;
                case ".png":
                default:
                    return ImageFormat.Png; // デフォルトはPNG
            }
        }

        /// <summary>
        /// 古いリサイズ済み画像ファイルを削除する。
        /// </summary>
        /// <param name="resizedImagePath">削除する画像ファイルのパス</param>
        public void DeleteResizedImage(string resizedImagePath)
        {
            if (string.IsNullOrEmpty(resizedImagePath)) return;
            if (!File.Exists(resizedImagePath)) return;

            try
            {
                File.Delete(resizedImagePath);
                System.Diagnostics.Debug.WriteLine($"リサイズ済み画像を削除しました: {resizedImagePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"リサイズ済み画像の削除に失敗: {ex.Message}");
                // エラーが発生しても処理は続行
            }
        }

        /// <summary>
        /// 画像を指定した高さにリサイズし、ユーザーが指定したパスに保存する。
        /// SaveFileDialogで保存先を選択させる。
        /// </summary>
        /// <param name="originalImagePath">元画像のファイルパス</param>
        /// <param name="newHeight">リサイズ後の高さ（px）</param>
        /// <param name="suggestedFileName">推奨ファイル名（拡張子なし）</param>
        /// <returns>保存したパス、キャンセル時はnull</returns>
        public string ResizeAndSaveToUserLocation(string originalImagePath, int newHeight, string suggestedFileName = "resized_image")
        {
            if (string.IsNullOrEmpty(originalImagePath))
                throw new ArgumentNullException(nameof(originalImagePath));

            if (!File.Exists(originalImagePath))
                throw new FileNotFoundException("元画像ファイルが見つかりません。", originalImagePath);

            // 元画像の拡張子を取得
            string originalExt = Path.GetExtension(originalImagePath).ToLower();

            // GIFアニメーションの場合はリサイズしない
            if (IsAnimatedGif(originalImagePath))
            {
                System.Windows.Forms.MessageBox.Show(
                    "アニメーションGIFはリサイズできません。",
                    "情報",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
                return null;
            }

            try
            {
                // 元画像を読み込み
                using (Image originalImage = Image.FromFile(originalImagePath))
                {
                    // リサイズ実行
                    using (Bitmap resizedBitmap = ResizeImage(originalImage, newHeight))
                    {
                        // SaveFileDialogで保存先を選択
                        using (var saveDialog = new System.Windows.Forms.SaveFileDialog())
                        {
                            // デフォルトのファイル名を設定
                            saveDialog.FileName = $"{suggestedFileName}_{newHeight}px";

                            // フィルタ設定
                            saveDialog.Filter = "PNG画像|*.png|JPEG画像|*.jpg;*.jpeg|すべてのファイル|*.*";
                            saveDialog.FilterIndex = 1; // デフォルトはPNG
                            saveDialog.Title = "リサイズ済み画像を保存";
                            saveDialog.DefaultExt = "png";

                            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                string savePath = saveDialog.FileName;
                                string ext = Path.GetExtension(savePath).ToLower();

                                // 画像形式を決定
                                ImageFormat format = GetImageFormat(ext);

                                // ファイルに保存
                                resizedBitmap.Save(savePath, format);

                                System.Diagnostics.Debug.WriteLine($"ユーザー指定場所に保存: {savePath}");

                                System.Windows.Forms.MessageBox.Show(
                                    $"リサイズ済み画像を保存しました。\n\n保存先: {savePath}",
                                    "保存完了",
                                    System.Windows.Forms.MessageBoxButtons.OK,
                                    System.Windows.Forms.MessageBoxIcon.Information);

                                return savePath;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("保存がキャンセルされました");
                                return null;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"画像保存エラー: {ex.Message}");
                System.Windows.Forms.MessageBox.Show(
                    $"画像の保存に失敗しました:\n{ex.Message}",
                    "エラー",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// 元画像のサイズ（幅x高さ）を取得する。
        /// </summary>
        /// <param name="imagePath">画像ファイルのパス</param>
        /// <returns>画像サイズ（Size構造体）、失敗時はSize.Empty</returns>
        public Size GetImageSize(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return Size.Empty;

            try
            {
                using (Image image = Image.FromFile(imagePath))
                {
                    return image.Size;
                }
            }
            catch
            {
                return Size.Empty;
            }
        }

        /// <summary>
        /// 元画像をユーザーが指定した場所にそのまま保存する。
        /// </summary>
        /// <param name="originalImagePath">元画像のファイルパス</param>
        /// <param name="suggestedFileName">推奨ファイル名</param>
        /// <returns>保存したパス、キャンセル時はnull</returns>
        public string SaveOriginalImageToUserLocation(string originalImagePath, string suggestedFileName)
        {
            if (string.IsNullOrEmpty(originalImagePath))
                return null;

            if (!File.Exists(originalImagePath))
                return null;

            try
            {
                using (var saveDialog = new System.Windows.Forms.SaveFileDialog())
                {
                    // 元画像の拡張子を取得
                    string originalExt = Path.GetExtension(originalImagePath).ToLower();
                    string defaultExt = string.IsNullOrEmpty(originalExt) ? ".png" : originalExt.TrimStart('.');

                    // ファイル名をシンプルに
                    saveDialog.FileName = suggestedFileName;
                    saveDialog.Filter = "PNG画像|*.png|JPEG画像|*.jpg;*.jpeg|GIF画像|*.gif|BMP画像|*.bmp|すべてのファイル|*.*";

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
                        case ".bmp":
                            saveDialog.FilterIndex = 4; // BMP
                            break;
                        default:
                            saveDialog.FilterIndex = 1; // デフォルトはPNG
                            break;
                    }

                    saveDialog.Title = "元画像を保存";
                    saveDialog.DefaultExt = defaultExt;

                    if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        // 元画像をコピー
                        File.Copy(originalImagePath, saveDialog.FileName, true);

                        System.Diagnostics.Debug.WriteLine($"元画像を保存: {saveDialog.FileName}");

                        System.Windows.Forms.MessageBox.Show(
                            $"元画像を保存しました。\n\n保存先: {saveDialog.FileName}",
                            "保存完了",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Information);

                        return saveDialog.FileName;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"元画像保存エラー: {ex.Message}");
                System.Windows.Forms.MessageBox.Show(
                    $"元画像の保存に失敗しました:\n{ex.Message}",
                    "エラー",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                return null;
            }
        }
    }
}