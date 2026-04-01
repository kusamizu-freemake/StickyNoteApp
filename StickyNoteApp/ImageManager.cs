using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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
        // 数値定数
        private const int DEFAULT_IMAGE_HEIGHT = 150;
        private const int IMAGE_LEFT_MARGIN = 0;
        private const long MAX_FILE_SIZE_BYTES = 2 * 1024 * 1024; // 2MB
        private const int MIN_TEXT_AREA_HEIGHT = 80; // テキストが見えるための最小高さ
        private const int DEFAULT_TITLE_BAR_HEIGHT = 40;
        private const int DELETE_RETRY_WAIT_MS = 100;

        // ファイル関連定数
        private const string IMAGE_FOLDER_NAME = "Images";
        private const string APP_FOLDER_NAME = "StickyNoteApp";
        private const string ORIGINAL_FILE_NAME_FORMAT = "{0}_original_{1:yyyyMMddHHmmss}{2}";
        private const string EXT_GIF = ".gif";
        private const string EXT_PNG = ".png";

        // ログメッセージ定数
        private const string MSG_DELETE_FILE_ERROR = "画像ファイル削除時にエラーが発生: {0}";
        private const string MSG_DELETE_SUCCESS = "画像削除成功: {0}";
        private const string MSG_DELETE_RETRY = "画像削除リトライ {0}/{1}: {2}";
        private const string MSG_DELETE_FINAL_FAIL = "画像削除失敗（最終リトライ）: {0} - {1}";
        private const string MSG_LOAD_ERROR = "画像読み込みエラー: {0}";
        private const string MSG_AUTO_ADJUST = "付箋サイズを自動調整: {0}px (画像: {1}px, テキスト領域: {2}px確保)";

        // ダイアログ・メッセージ定数
        private const string MSG_FILE_SIZE_OVER = "選択した画像ファイルのサイズが大きすぎます。\nファイルサイズ: {0:F2}MB\n最大サイズ: 2MB\n\n2MB以下の画像ファイルを選択してください。";
        private const string TITLE_FILE_SIZE_ERROR = "ファイルサイズエラー";
        private const string MSG_LOAD_SUCCESS = "画像を読み込みました。";
        private const string TITLE_COMPLETE = "完了";
        private const string MSG_LOAD_FAILED = "画像の読み込みに失敗しました:\n{0}";
        private const string TITLE_ERROR = "エラー";

        // ファイルダイアログ定数
        private const string FILTER_OPEN_IMAGE = "画像ファイル|*.png;*.jpg;*.jpeg;*.gif|PNGファイル|*.png|JPEGファイル|*.jpg;*.jpeg|GIFファイル|*.gif|すべてのファイル|*.*";
        private const string TITLE_OPEN_IMAGE = "画像を選択";

        // コントロール名定数
        private const string CONTROL_TITLE_BAR = "titleBar";

        private readonly StickyNoteForm ParentForm;
        private readonly PictureBox PictureBox;

        // 画像パス管理
        private string OriginalImagePathValue;                           // 元画像のパス
        private string ResizedImagePathValue;                            // リサイズ済み画像のパス
        private int ImageDisplayHeightValue = DEFAULT_IMAGE_HEIGHT;   // 画像表示高さ

        public string OriginalImagePath => OriginalImagePathValue;
        public string ResizedImagePath => ResizedImagePathValue;
        public int ImageDisplayHeight => ImageDisplayHeightValue;
        public string CapturedImagePath => ResizedImagePathValue ?? OriginalImagePathValue; // 互換性

        /// <summary>
        /// 画像の表示高さを更新する（ImageResizeManagerから呼び出し）
        /// </summary>
        public void SetImageDisplayHeight(int Height)
        {
            ImageDisplayHeightValue = Height;
        }

        /// <summary>
        /// 元画像のパスを取得する
        /// </summary>
        public string GetOriginalImagePath()
        {
            return OriginalImagePathValue;
        }

        /// <summary>
        /// リサイズ済み画像のパスを取得する
        /// </summary>
        public string GetResizedImagePath()
        {
            return ResizedImagePathValue;
        }

        /// <summary>
        /// リサイズ済み画像のパスを設定する
        /// </summary>
        public void SetResizedImagePath(string Path)
        {
            ResizedImagePathValue = Path;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ImageManager(StickyNoteForm Form, PictureBox PicBox)
        {
            ParentForm = Form;
            PictureBox = PicBox;
        }

        /// <summary>
        /// ファイルから画像を選択して読み込み
        /// </summary>
        public void SelectAndLoadImageFromFile()
        {
            try
            {
                using (var OpenDialog = new OpenFileDialog())
                {
                    OpenDialog.Filter = FILTER_OPEN_IMAGE;
                    OpenDialog.Title = TITLE_OPEN_IMAGE;
                    OpenDialog.Multiselect = false;

                    if (OpenDialog.ShowDialog() == DialogResult.OK)
                    {
                        // ファイルサイズをチェック
                        FileInfo FileInfo = new FileInfo(OpenDialog.FileName);
                        if (FileInfo.Length > MAX_FILE_SIZE_BYTES)
                        {
                            double FileSizeMB = FileInfo.Length / (1024.0 * 1024.0);
                            // サイズ超過の警告(小数点以下2桁まで表示 )
                            MessageBox.Show(
                                string.Format(MSG_FILE_SIZE_OVER, FileSizeMB),
                                TITLE_FILE_SIZE_ERROR,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        // 画像を読み込み
                        using (var OriginalImage = Image.FromFile(OpenDialog.FileName))
                        {
                            // Bitmapに変換してコピー
                            Bitmap BitmapImg = new Bitmap(OriginalImage);

                            // 既存の画像を削除
                            RemoveImageInternal();

                            // 元画像として保存
                            string ImagePath = SaveOriginalImage(BitmapImg, OpenDialog.FileName);
                            OriginalImagePathValue = ImagePath;
                            ResizedImagePathValue = null;

                            // PictureBoxに表示
                            LoadImageToPictureBox(ImagePath);
                            PictureBox.Height = DEFAULT_IMAGE_HEIGHT;
                            PictureBox.Visible = true;
                            ImageDisplayHeightValue = DEFAULT_IMAGE_HEIGHT;

                            // テキストボックスの位置を調整し、必要に応じて付箋サイズを拡大
                            AdjustTextBoxPosition();
                            EnsureTextAreaVisible();

                            // 画像読み込み完了時に保存
                            ParentForm.SaveCurrentNoteState();

                            MessageBox.Show(MSG_LOAD_SUCCESS, TITLE_COMPLETE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(
                    string.Format(MSG_LOAD_FAILED, Ex.Message),
                    TITLE_ERROR,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// メッセージボックスを表示せずに画像を削除
        /// </summary>
        private void RemoveImageInternal()
        {
            if (PictureBox.Image != null)
            {
                // アニメーションを停止
                ImageAnimator.StopAnimate(PictureBox.Image, OnFrameChanged);

                var Img = PictureBox.Image;
                PictureBox.Image = null;
                Img.Dispose();
            }
        }

        /// <summary>
        /// 画像を削除
        /// </summary>
        public void RemoveImage()
        {
            // 先に画像を解放（順序が重要）
            if (PictureBox.Image != null)
            {
                // アニメーションを停止
                ImageAnimator.StopAnimate(PictureBox.Image, OnFrameChanged);

                var Img = PictureBox.Image;
                PictureBox.Image = null;
                Img.Dispose();
            }

            PictureBox.Height = 0;
            PictureBox.Visible = false;

            // ファイル削除（リトライ機能付き）
            try
            {
                DeleteImageFileWithRetry(OriginalImagePathValue);
                DeleteImageFileWithRetry(ResizedImagePathValue);
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(string.Format(MSG_DELETE_FILE_ERROR, Ex.Message));
            }

            OriginalImagePathValue = null;
            ResizedImagePathValue = null;
            ImageDisplayHeightValue = DEFAULT_IMAGE_HEIGHT;

            // テキストボックスの位置を調整
            ParentForm.txtNote.Dock = DockStyle.Fill;

            // 画像削除完了時に保存
            ParentForm.SaveCurrentNoteState();
        }

        /// <summary>
        /// 画像を復元（データベースから読み込み時用）
        /// </summary>
        public void LoadImage(string ImagePath, string OriginalPath, string ResizedPath, int DisplayHeight)
        {
            try
            {
                OriginalImagePathValue = OriginalPath;
                ResizedImagePathValue = ResizedPath;
                ImageDisplayHeightValue = DisplayHeight > 0 ? DisplayHeight : DEFAULT_IMAGE_HEIGHT;

                // 表示する画像を決定
                string ImageToLoad = null;
                if (!string.IsNullOrEmpty(ResizedPath) && File.Exists(ResizedPath))
                {
                    ImageToLoad = ResizedPath;
                }
                else if (!string.IsNullOrEmpty(OriginalPath) && File.Exists(OriginalPath))
                {
                    ImageToLoad = OriginalPath;
                }
                else if (!string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath))
                {
                    ImageToLoad = ImagePath;
                    OriginalImagePathValue = ImagePath;
                }

                if (!string.IsNullOrEmpty(ImageToLoad))
                {
                    // 自動的にアニメーションが開始
                    LoadImageToPictureBox(ImageToLoad);
                    PictureBox.Height = ImageDisplayHeightValue;
                    PictureBox.Visible = true;

                    AdjustTextBoxPosition();
                    // 復元時も必要に応じて付箋サイズを調整
                    EnsureTextAreaVisible();
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(string.Format(MSG_LOAD_ERROR, Ex.Message));
            }
        }

        /// <summary>
        /// 元画像をファイルとして保存
        /// </summary>
        private string SaveOriginalImage(Bitmap Image, string SourceFilePath)
        {
            string Folder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                APP_FOLDER_NAME,
                IMAGE_FOLDER_NAME
            );

            if (!Directory.Exists(Folder))
            {
                Directory.CreateDirectory(Folder);
            }

            // ファイル拡張子を取得（GIFの場合はGIFで保存）
            string SourceExt = System.IO.Path.GetExtension(SourceFilePath).ToLower();
            string Ext = (SourceExt == EXT_GIF) ? EXT_GIF : EXT_PNG;

            string FileName = string.Format(ORIGINAL_FILE_NAME_FORMAT, ParentForm.NoteId, DateTime.Now, Ext);
            string FilePath = System.IO.Path.Combine(Folder, FileName);

            // GIFの場合は元ファイルをコピー（アニメーション情報を保持）
            if (SourceExt == EXT_GIF)
            {
                File.Copy(SourceFilePath, FilePath, true);
            }
            else
            {
                Image.Save(FilePath, ImageFormat.Png);
            }

            return FilePath;
        }

        /// <summary>
        /// PictureBoxに画像を読み込み、アニメーションGIFに対応
        /// </summary>
        private void LoadImageToPictureBox(string ImagePath)
        {
            // 既存の画像のアニメーションを停止
            if (PictureBox.Image != null)
            {
                ImageAnimator.StopAnimate(PictureBox.Image, OnFrameChanged);
                PictureBox.Image.Dispose();
            }

            // 新しい画像を読み込み
            PictureBox.Image = Image.FromFile(ImagePath);

            // アニメーションGIFの場合はアニメーションを開始
            if (IsAnimatedGif(PictureBox.Image))
            {
                ImageAnimator.Animate(PictureBox.Image, OnFrameChanged);
            }
        }

        /// <summary>
        /// アニメーションGIFかどうかを判定
        /// </summary>
        private bool IsAnimatedGif(Image Img)
        {
            if (Img == null) return false;

            // 静止画GIFはフレーム数が1, アニメーションGIFはフレーム数が2以上
            if (Img.RawFormat.Guid == ImageFormat.Gif.Guid)
            {
                var Dimension = new FrameDimension(Img.FrameDimensionsList[0]);
                int FrameCount = Img.GetFrameCount(Dimension);
                return FrameCount > 1;
            }
            return false;
        }

        /// <summary>
        /// アニメーションフレーム更新時のコールバック
        /// </summary>
        private void OnFrameChanged(object Sender, EventArgs E)
        {
            if (PictureBox.InvokeRequired)
            {
                // UIスレッドで実行
                PictureBox.BeginInvoke(new EventHandler(OnFrameChanged), Sender, E);
            }
            else
            {
                // フレームを更新して再描画
                ImageAnimator.UpdateFrames(PictureBox.Image);
                PictureBox.Invalidate();
            }
        }

        /// <summary>
        /// 画像ファイルをリトライ付きで削除
        /// </summary>
        private void DeleteImageFileWithRetry(string FilePath, int MaxRetries = 3)
        {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
                return;

            for (int I = 0; I < MaxRetries; I++)
            {
                try
                {
                    File.Delete(FilePath);
                    Debug.WriteLine(string.Format(MSG_DELETE_SUCCESS, FilePath));
                    return;
                }
                catch (IOException Ex)
                {
                    if (I < MaxRetries - 1)
                    {
                        Debug.WriteLine(string.Format(MSG_DELETE_RETRY, I + 1, MaxRetries, FilePath));
                        System.Threading.Thread.Sleep(DELETE_RETRY_WAIT_MS);
                    }
                    else
                    {
                        Debug.WriteLine(string.Format(MSG_DELETE_FINAL_FAIL, FilePath, Ex.Message));
                    }
                }
            }
        }

        /// <summary>
        /// テキストボックスの位置を調整
        /// </summary>
        public void AdjustTextBoxPosition()
        {
            // 画像が非表示のときは Dock=Fill のまま何もしない
            // Dock を None に変えると txtNote.Top=0 になり,タイトルバーを覆い隠すバグが発生する
            if (!PictureBox.Visible)
            {
                ParentForm.txtNote.Dock = DockStyle.Fill;
                return;
            }

            ParentForm.txtNote.Dock = DockStyle.None;
            ParentForm.txtNote.Top = PictureBox.Bottom;
            ParentForm.txtNote.Left = IMAGE_LEFT_MARGIN;
            ParentForm.txtNote.Width = ParentForm.ClientSize.Width;
            ParentForm.txtNote.Height = ParentForm.ClientSize.Height - ParentForm.txtNote.Top;
        }

        /// <summary>
        /// テキスト領域が十分に表示されるように付箋サイズを調整
        /// 画像添付時にテキストが見えなくならないようにする
        /// </summary>
        private void EnsureTextAreaVisible()
        {
            // タイトルバーの高さを取得
            int TitleBarHeight = ParentForm.Controls[CONTROL_TITLE_BAR]?.Height ?? DEFAULT_TITLE_BAR_HEIGHT;

            // 現在のテキスト領域の高さを計算
            int CurrentTextHeight = ParentForm.ClientSize.Height - TitleBarHeight - PictureBox.Height;

            // テキスト領域が最小高さより小さい場合、付箋を拡大
            if (CurrentTextHeight < MIN_TEXT_AREA_HEIGHT)
            {
                int RequiredHeight = TitleBarHeight + PictureBox.Height + MIN_TEXT_AREA_HEIGHT;
                ParentForm.ClientSize = new Size(ParentForm.ClientSize.Width, RequiredHeight);

                // サイズ変更後、再度テキストボックスの位置を調整
                AdjustTextBoxPosition();

                Debug.WriteLine(string.Format(MSG_AUTO_ADJUST,
                    ParentForm.ClientSize.Height, PictureBox.Height, MIN_TEXT_AREA_HEIGHT));
            }
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        public void Dispose()
        {
            if (PictureBox != null && PictureBox.Image != null)
            {
                // アニメーションを停止
                ImageAnimator.StopAnimate(PictureBox.Image, OnFrameChanged);

                var Img = PictureBox.Image;
                PictureBox.Image = null;
                Img.Dispose();
            }
        }
    }
}