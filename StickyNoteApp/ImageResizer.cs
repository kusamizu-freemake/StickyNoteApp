using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// 画像ファイルを実際にリサイズして保存するクラス。
    /// アスペクト比を維持してリサイズを行い、指定されたパスに保存する。
    /// </summary>
    public class ImageResizer
    {
        // ログメッセージ定数
        private const string MSG_SKIP_ANIMATED_GIF = "アニメーションGIFはリサイズをスキップします: {0}";
        private const string MSG_RESIZE_SAVED = "リサイズ画像を保存しました: {0}";
        private const string MSG_RESIZE_ERROR = "画像リサイズエラー: {0}";
        private const string MSG_RESIZE_ERROR_THROW = "画像のリサイズに失敗しました: {0}";
        private const string MSG_DELETE_RESIZED = "リサイズ済み画像を削除しました: {0}";
        private const string MSG_DELETE_RESIZED_FAIL = "リサイズ済み画像の削除に失敗: {0}";
        private const string MSG_SAVE_CANCELLED = "保存がキャンセルされました";
        private const string MSG_SAVE_TO_USER = "ユーザー指定場所に保存: {0}";
        private const string MSG_SAVE_ERROR = "画像保存エラー: {0}";
        private const string MSG_SAVE_ORIGINAL = "元画像を保存: {0}";
        private const string MSG_SAVE_ORIGINAL_ERROR = "元画像保存エラー: {0}";

        // ダイアログ・メッセージ定数
        private const string MSG_ANIMATED_GIF_CANNOT_RESIZE = "アニメーションGIFはリサイズできません。";
        private const string TITLE_INFO = "情報";
        private const string MSG_SAVE_COMPLETE = "リサイズ済み画像を保存しました。\n\n保存先: {0}";
        private const string TITLE_SAVE_COMPLETE = "保存完了";
        private const string MSG_SAVE_FAILED = "画像の保存に失敗しました:\n{0}";
        private const string TITLE_ERROR = "エラー";
        private const string MSG_ORIGINAL_SAVE_COMPLETE = "元画像を保存しました。\n\n保存先: {0}";
        private const string MSG_ORIGINAL_SAVE_FAILED = "元画像の保存に失敗しました:\n{0}";

        // ファイルダイアログ定数
        private const string FILTER_RESIZE_IMAGE = "PNG画像|*.png|JPEG画像|*.jpg;*.jpeg|すべてのファイル|*.*";
        private const string FILTER_ORIGINAL_IMAGE = "PNG画像|*.png|JPEG画像|*.jpg;*.jpeg|GIF画像|*.gif|BMP画像|*.bmp|すべてのファイル|*.*";
        private const string TITLE_SAVE_RESIZED = "リサイズ済み画像を保存";
        private const string TITLE_SAVE_ORIGINAL = "元画像を保存";
        private const string DEFAULT_EXT_PNG = "png";

        // ファイル名フォーマット定数
        private const string RESIZED_FILE_NAME_FORMAT = "{0}_resized_{1}px_{2:yyyyMMddHHmmss}{3}";
        private const string RESIZED_SUGGEST_FORMAT = "{0}_{1}px";

        // 拡張子定数
        private const string EXT_GIF = ".gif";
        private const string EXT_PNG = ".png";
        private const string EXT_JPG = ".jpg";
        private const string EXT_JPEG = ".jpeg";
        private const string EXT_BMP = ".bmp";

        // 引数エラーメッセージ定数
        private const string ARG_ERR_IMAGE_NULL = "高さは正の値である必要があります。";
        private const string ARG_ERR_FILE_NOT_FOUND = "元画像ファイルが見つかりません。";

        // 数値定数
        private const int MIN_WIDTH = 1;

        private readonly string ImageFolder;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="Folder">リサイズ済み画像の保存先フォルダ</param>
        public ImageResizer(string Folder)
        {
            ImageFolder = Folder;

            // フォルダが存在しない場合は作成
            if (!Directory.Exists(ImageFolder))
            {
                Directory.CreateDirectory(ImageFolder);
            }
        }

        /// <summary>
        /// リサイズ後の画像保存先フォルダを取得
        /// </summary>
        private string GetResizedImageFolder()
        {
            return ImageFolder;
        }

        /// <summary>
        /// 画像を指定した高さにリサイズし、アスペクト比を維持する。
        /// </summary>
        /// <param name="OriginalImage">元画像</param>
        /// <param name="NewHeight">リサイズ後の高さ（px）</param>
        /// <returns>リサイズされたBitmap（呼び出し側でDisposeする必要がある）</returns>
        public Bitmap ResizeImage(Image OriginalImage, int NewHeight)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException(nameof(OriginalImage));

            if (NewHeight <= 0)
                throw new ArgumentException(ARG_ERR_IMAGE_NULL, nameof(NewHeight));

            // アスペクト比を維持して幅を計算
            int NewWidth = (int)(OriginalImage.Width * ((double)NewHeight / OriginalImage.Height));

            // 幅が0にならないようにする
            if (NewWidth <= 0) NewWidth = MIN_WIDTH;

            // リサイズ後の画像を作成
            Bitmap ResizedBitmap = new Bitmap(NewWidth, NewHeight);

            using (Graphics Gfx = Graphics.FromImage(ResizedBitmap))
            {
                // 画像を描画（リサイズ）
                Gfx.DrawImage(OriginalImage, 0, 0, NewWidth, NewHeight);
            }

            return ResizedBitmap;
        }

        /// <summary>
        /// 元画像ファイルを読み込み、指定した高さにリサイズして保存する。
        /// </summary>
        /// <param name="OriginalImagePath">元画像のファイルパス</param>
        /// <param name="NewHeight">リサイズ後の高さ（px）</param>
        /// <param name="NoteId">付箋ID（ファイル名生成に使用）</param>
        /// <returns>リサイズ後の画像の保存先パス</returns>
        public string ResizeAndSaveImage(string OriginalImagePath, int NewHeight, string NoteId)
        {
            if (string.IsNullOrEmpty(OriginalImagePath))
                throw new ArgumentNullException(nameof(OriginalImagePath));

            if (!File.Exists(OriginalImagePath))
                throw new FileNotFoundException(ARG_ERR_FILE_NOT_FOUND, OriginalImagePath);

            // 元画像の拡張子を取得
            string OriginalExt = Path.GetExtension(OriginalImagePath).ToLower();

            // GIFアニメーションの場合はリサイズしない（アニメーション情報が失われるため）
            if (IsAnimatedGif(OriginalImagePath))
            {
                System.Diagnostics.Debug.WriteLine(string.Format(MSG_SKIP_ANIMATED_GIF, OriginalImagePath));
                return null; // リサイズしない場合はnullを返す
            }

            try
            {
                // 元画像を読み込み
                using (Image OriginalImage = Image.FromFile(OriginalImagePath))
                {
                    // リサイズ実行
                    using (Bitmap ResizedBitmap = ResizeImage(OriginalImage, NewHeight))
                    {
                        // 保存先フォルダを確保
                        string Folder = GetResizedImageFolder();
                        if (!Directory.Exists(Folder))
                        {
                            Directory.CreateDirectory(Folder);
                        }

                        // 保存ファイル名を生成（付箋ID_resized_高さpx_タイムスタンプ.拡張子）
                        string Ext = (OriginalExt == EXT_GIF) ? EXT_GIF : EXT_PNG;
                        string FileName = string.Format(RESIZED_FILE_NAME_FORMAT, NoteId, NewHeight, DateTime.Now, Ext);
                        string SavePath = Path.Combine(Folder, FileName);

                        // 画像形式を決定してファイルに保存
                        ResizedBitmap.Save(SavePath, GetImageFormat(Ext));

                        System.Diagnostics.Debug.WriteLine(string.Format(MSG_RESIZE_SAVED, SavePath));
                        return SavePath;
                    }
                }
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format(MSG_RESIZE_ERROR, Ex.Message));
                throw new Exception(string.Format(MSG_RESIZE_ERROR_THROW, Ex.Message), Ex);
            }
        }

        /// <summary>
        /// アニメーションGIFかどうかを判定する。
        /// </summary>
        private bool IsAnimatedGif(string ImagePath)
        {
            try
            {
                using (Image Img = Image.FromFile(ImagePath))
                {
                    // GIF形式かチェック
                    if (Img.RawFormat.Guid != ImageFormat.Gif.Guid)
                        return false;

                    // フレーム数をチェック
                    var Dimension = new FrameDimension(Img.FrameDimensionsList[0]);
                    int FrameCount = Img.GetFrameCount(Dimension);
                    return FrameCount > 1; // フレーム数が2以上ならアニメーションGIF
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
        private ImageFormat GetImageFormat(string Extension)
        {
            switch (Extension.ToLower())
            {
                case EXT_JPG:
                case EXT_JPEG:
                    return ImageFormat.Jpeg;
                case EXT_GIF:
                    return ImageFormat.Gif;
                case EXT_BMP:
                    return ImageFormat.Bmp;
                case EXT_PNG:
                default:
                    return ImageFormat.Png; // デフォルトはPNG
            }
        }

        /// <summary>
        /// 古いリサイズ済み画像ファイルを削除する。
        /// </summary>
        /// <param name="ResizedImagePath">削除する画像ファイルのパス</param>
        public void DeleteResizedImage(string ResizedImagePath)
        {
            if (string.IsNullOrEmpty(ResizedImagePath)) return;
            if (!File.Exists(ResizedImagePath)) return;

            try
            {
                File.Delete(ResizedImagePath);
                System.Diagnostics.Debug.WriteLine(string.Format(MSG_DELETE_RESIZED, ResizedImagePath));
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format(MSG_DELETE_RESIZED_FAIL, Ex.Message));
                // エラーが発生しても処理は続行
            }
        }

        /// <summary>
        /// 画像を指定した高さにリサイズし、ユーザーが指定したパスに保存する。
        /// SaveFileDialogで保存先を選択させる。
        /// </summary>
        /// <param name="OriginalImagePath">元画像のファイルパス</param>
        /// <param name="NewHeight">リサイズ後の高さ（px）</param>
        /// <param name="SuggestedFileName">推奨ファイル名（拡張子なし）</param>
        /// <returns>保存したパス、キャンセル時はnull</returns>
        public string ResizeAndSaveToUserLocation(string OriginalImagePath, int NewHeight, string SuggestedFileName = "resized_image")
        {
            if (string.IsNullOrEmpty(OriginalImagePath))
                throw new ArgumentNullException(nameof(OriginalImagePath));

            if (!File.Exists(OriginalImagePath))
                throw new FileNotFoundException(ARG_ERR_FILE_NOT_FOUND, OriginalImagePath);

            // GIFアニメーションの場合はリサイズしない
            if (IsAnimatedGif(OriginalImagePath))
            {
                MessageBox.Show(MSG_ANIMATED_GIF_CANNOT_RESIZE, TITLE_INFO, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            try
            {
                // 元画像を読み込み
                using (Image OriginalImage = Image.FromFile(OriginalImagePath))
                {
                    // リサイズ実行
                    using (Bitmap ResizedBitmap = ResizeImage(OriginalImage, NewHeight))
                    {
                        // SaveFileDialogで保存先を選択
                        using (var SaveDialog = new SaveFileDialog())
                        {
                            SaveDialog.FileName = string.Format(RESIZED_SUGGEST_FORMAT, SuggestedFileName, NewHeight);
                            SaveDialog.Filter = FILTER_RESIZE_IMAGE;
                            SaveDialog.FilterIndex = 1; // デフォルトはPNG
                            SaveDialog.Title = TITLE_SAVE_RESIZED;
                            SaveDialog.DefaultExt = DEFAULT_EXT_PNG;

                            if (SaveDialog.ShowDialog() == DialogResult.OK)
                            {
                                string SavePath = SaveDialog.FileName;
                                string Ext = Path.GetExtension(SavePath).ToLower();
                                // ファイルに保存
                                ResizedBitmap.Save(SavePath, GetImageFormat(Ext));

                                System.Diagnostics.Debug.WriteLine(string.Format(MSG_SAVE_TO_USER, SavePath));

                                MessageBox.Show(
                                    string.Format(MSG_SAVE_COMPLETE, SavePath),
                                    TITLE_SAVE_COMPLETE,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);

                                return SavePath;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine(MSG_SAVE_CANCELLED);
                                return null;
                            }
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format(MSG_SAVE_ERROR, Ex.Message));
                MessageBox.Show(
                    string.Format(MSG_SAVE_FAILED, Ex.Message),
                    TITLE_ERROR,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// 元画像のサイズ（幅x高さ）を取得する。
        /// </summary>
        /// <param name="ImagePath">画像ファイルのパス</param>
        /// <returns>画像サイズ（Size構造体）、失敗時はSize.Empty</returns>
        public Size GetImageSize(string ImagePath)
        {
            if (string.IsNullOrEmpty(ImagePath) || !File.Exists(ImagePath))
                return Size.Empty;

            try
            {
                using (Image Img = Image.FromFile(ImagePath))
                {
                    return Img.Size;
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
        /// <param name="OriginalImagePath">元画像のファイルパス</param>
        /// <param name="SuggestedFileName">推奨ファイル名</param>
        /// <returns>保存したパス、キャンセル時はnull</returns>
        public string SaveOriginalImageToUserLocation(string OriginalImagePath, string SuggestedFileName)
        {
            if (string.IsNullOrEmpty(OriginalImagePath)) return null;
            if (!File.Exists(OriginalImagePath)) return null;

            try
            {
                using (var SaveDialog = new SaveFileDialog())
                {
                    // 元画像の拡張子を取得
                    string OriginalExt = Path.GetExtension(OriginalImagePath).ToLower();
                    string DefaultExt = string.IsNullOrEmpty(OriginalExt) ? DEFAULT_EXT_PNG : OriginalExt.TrimStart('.');

                    SaveDialog.FileName = SuggestedFileName;
                    SaveDialog.Filter = FILTER_ORIGINAL_IMAGE;
                    SaveDialog.Title = TITLE_SAVE_ORIGINAL;
                    SaveDialog.DefaultExt = DefaultExt;

                    // 元画像の拡張子に応じてFilterIndexを設定
                    switch (OriginalExt)
                    {
                        case EXT_PNG: SaveDialog.FilterIndex = 1; break;
                        case EXT_JPG:
                        case EXT_JPEG: SaveDialog.FilterIndex = 2; break;
                        case EXT_GIF: SaveDialog.FilterIndex = 3; break;
                        case EXT_BMP: SaveDialog.FilterIndex = 4; break;
                        default: SaveDialog.FilterIndex = 1; break; // デフォルトはPNG
                    }

                    if (SaveDialog.ShowDialog() == DialogResult.OK)
                    {
                        // 元画像をコピー
                        File.Copy(OriginalImagePath, SaveDialog.FileName, true);

                        System.Diagnostics.Debug.WriteLine(string.Format(MSG_SAVE_ORIGINAL, SaveDialog.FileName));

                        MessageBox.Show(
                            string.Format(MSG_ORIGINAL_SAVE_COMPLETE, SaveDialog.FileName),
                            TITLE_SAVE_COMPLETE,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        return SaveDialog.FileName;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format(MSG_SAVE_ORIGINAL_ERROR, Ex.Message));
                MessageBox.Show(
                    string.Format(MSG_ORIGINAL_SAVE_FAILED, Ex.Message),
                    TITLE_ERROR,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return null;
            }
        }
    }
}