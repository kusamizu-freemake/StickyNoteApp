using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// 付箋上の画像のサイズ変更を担当するマネージャークラス。
    /// サイズ定数の管理、メニュー項目の生成、サイズ変更の実行を行う。
    /// </summary>
    public class ImageResizeManager
    {
        // サイズ定数（唯一のソース）
        public const int SIZE_SMALL = 100; // 小
        public const int SIZE_MEDIUM = 150; // 中（デフォルト）
        public const int SIZE_LARGE = 200; // 大
        public const int SIZE_EXTRA_LARGE = 250; // 特大

        // 数値定数
        private const int MIN_TEXT_AREA_HEIGHT = 80; // テキスト領域の最小高さ
        private const int DEFAULT_TITLE_BAR_HEIGHT = 40;

        // ログメッセージ定数
        private const string MSG_NO_ORIGINAL_PATH = "元画像パスが設定されていません";
        private const string MSG_RESIZE_SUCCESS = "リサイズ成功: {0}";
        private const string MSG_RESIZE_SKIPPED = "リサイズをスキップしました（アニメーションGIFまたはエラー）";
        private const string MSG_AUTO_ADJUST = "付箋サイズを自動調整: {0}px (画像: {1}px, テキスト領域: {2}px確保)";

        // ダイアログ・メッセージ定数
        private const string MSG_NO_IMAGE_TO_SAVE = "保存する画像がありません。";
        private const string TITLE_INFO = "情報";

        // メニューラベル定数
        private const string MENU_LABEL_SMALL = "小 (100px)";
        private const string MENU_LABEL_MEDIUM = "中 (150px)";
        private const string MENU_LABEL_LARGE = "大 (200px)";
        private const string MENU_LABEL_EXTRA_LARGE = "特大 (250px)";

        // コントロール名定数
        private const string CONTROL_TITLE_BAR = "titleBar";

        // 依存オブジェクト
        private readonly StickyNoteForm ParentForm;
        private readonly PictureBox PictureBox;
        private readonly ImageManager ImageManager;
        private readonly ImageResizer ImageResizer;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="Form">親付箋フォーム（保存メソッドの呼び出し元）</param>
        /// <param name="PicBox">画像を表示しているPictureBox</param>
        /// <param name="ImgManager">画像マネージャー（状態更新・レイアウト調整の委譲先）</param>
        /// <param name="ImgResizer">画像リサイザー（リサイズ処理の委譲先）</param>
        public ImageResizeManager(StickyNoteForm Form, PictureBox PicBox, ImageManager ImgManager, ImageResizer ImgResizer)
        {
            ParentForm = Form;
            PictureBox = PicBox;
            ImageManager = ImgManager;
            ImageResizer = ImgResizer;
        }

        /// <summary>
        /// 画像サイズを変更する。
        /// 実際に画像ファイルをリサイズして保存し、
        /// PictureBoxの高さを更新し、ImageManagerの表示高さと
        /// テキストボックスの位置も連動して調整する。
        /// </summary>
        /// <param name="Height">設定する高さ（px）</param>
        public void ResizeImage(int Height)
        {
            if (!PictureBox.Visible) return;

            // 1. 元画像パスと古いリサイズ済みパスを取得
            string OriginalPath = ImageManager.GetOriginalImagePath();
            string OldResizedPath = ImageManager.GetResizedImagePath();

            if (string.IsNullOrEmpty(OriginalPath))
            {
                Debug.WriteLine(MSG_NO_ORIGINAL_PATH);
                return;
            }

            // 2. 古いリサイズ済み画像を削除
            if (!string.IsNullOrEmpty(OldResizedPath))
            {
                ImageResizer.DeleteResizedImage(OldResizedPath);
            }

            // 3. 新しいサイズでリサイズ実行（ImageResizerに委譲）
            string NewResizedPath = ImageResizer.ResizeAndSaveImage(
                OriginalPath,
                Height,
                ParentForm.NoteId
            );

            if (NewResizedPath != null)
            {
                Debug.WriteLine(string.Format(MSG_RESIZE_SUCCESS, NewResizedPath));

                // 4. ImageManagerに新しいパスを設定
                ImageManager.SetResizedImagePath(NewResizedPath);
            }
            else
            {
                Debug.WriteLine(MSG_RESIZE_SKIPPED);
            }

            // 5. 表示サイズを変更
            PictureBox.Height = Height;
            ImageManager.SetImageDisplayHeight(Height);
            ImageManager.AdjustTextBoxPosition();

            // テキスト領域が十分に見えるように付箋サイズを調整
            EnsureTextAreaVisible(Height);

            // 6. 保存
            ParentForm.SaveCurrentNoteState();
        }

        /// <summary>
        /// テキスト領域が十分に表示されるように付箋サイズを調整
        /// 画像サイズ変更時にテキストが見えなくならないようにする
        /// </summary>
        /// <param name="ImageHeight">画像の高さ</param>
        private void EnsureTextAreaVisible(int ImageHeight)
        {
            // タイトルバーの高さを取得
            int TitleBarHeight = ParentForm.Controls[CONTROL_TITLE_BAR]?.Height ?? DEFAULT_TITLE_BAR_HEIGHT;

            // 現在のテキスト領域の高さを計算
            int CurrentTextHeight = ParentForm.ClientSize.Height - TitleBarHeight - ImageHeight;

            // テキスト領域が最小高さより小さい場合、付箋を拡大
            if (CurrentTextHeight < MIN_TEXT_AREA_HEIGHT)
            {
                int RequiredHeight = TitleBarHeight + ImageHeight + MIN_TEXT_AREA_HEIGHT;
                ParentForm.ClientSize = new Size(ParentForm.ClientSize.Width, RequiredHeight);

                // サイズ変更後、再度テキストボックスの位置を調整
                ImageManager.AdjustTextBoxPosition();

                Debug.WriteLine(string.Format(MSG_AUTO_ADJUST,
                    ParentForm.ClientSize.Height, ImageHeight, MIN_TEXT_AREA_HEIGHT));
            }
        }

        /// <summary>
        /// ユーザー指定場所に画像を保存する（表示サイズは変更しない）
        /// </summary>
        /// <param name="Height">リサイズ後の高さ（px）、0の場合は元のサイズで保存</param>
        public void SaveResizedImageToUserLocation(int Height)
        {
            string OriginalPath = ImageManager.GetOriginalImagePath();

            if (string.IsNullOrEmpty(OriginalPath))
            {
                MessageBox.Show(MSG_NO_IMAGE_TO_SAVE, TITLE_INFO, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 推奨ファイル名
            string SuggestedFileName = "image";

            if (Height == 0)
            {
                // 元のサイズで保存（ImageResizerに委譲）
                ImageResizer.SaveOriginalImageToUserLocation(OriginalPath, SuggestedFileName);
            }
            else
            {
                // リサイズして保存（ImageResizerに委譲）
                ImageResizer.ResizeAndSaveToUserLocation(OriginalPath, Height, SuggestedFileName);
            }
        }

        /// <summary>
        /// サイズ選択メニューの DropDownItems に項目を追加する。
        /// 呼び出し側で用意した ToolStripMenuItem（親メニュー）を渡す。
        /// </summary>
        /// <param name="ParentMenuItem">「画像サイズ」サブメニューの親項目</param>
        public void CreateSizeMenuItems(ToolStripMenuItem ParentMenuItem)
        {
            var Items = new (string Label, int Size)[]
            {
                (MENU_LABEL_SMALL,       SIZE_SMALL),
                (MENU_LABEL_MEDIUM,      SIZE_MEDIUM),
                (MENU_LABEL_LARGE,       SIZE_LARGE),
                (MENU_LABEL_EXTRA_LARGE, SIZE_EXTRA_LARGE),
            };

            foreach (var (Label, Size) in Items)
            {
                var MenuItem = new ToolStripMenuItem(Label);
                int CapturedSize = Size; // クロージャキャプチャ用
                MenuItem.Click += (S, E) => ResizeImage(CapturedSize);
                ParentMenuItem.DropDownItems.Add(MenuItem);
            }
        }
    }
}