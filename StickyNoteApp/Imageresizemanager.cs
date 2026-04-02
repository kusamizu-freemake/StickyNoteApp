using System;
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

        // テキスト領域の最小高さ
        private const int MIN_TEXT_AREA_HEIGHT = 80;

        // 依存オブジェクト
        private readonly StickyNoteForm parentForm;
        private readonly PictureBox pictureBox;
        private readonly ImageManager imageManager;
        private readonly ImageResizer imageResizer;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="form">親付箋フォーム（保存メソッドの呼び出し元）</param>
        /// <param name="picBox">画像を表示しているPictureBox</param>
        /// <param name="imgManager">画像マネージャー（状態更新・レイアウト調整の委譲先）</param>
        /// <param name="imgResizer">画像リサイザー（リサイズ処理の委譲先）</param>
        public ImageResizeManager(StickyNoteForm form, PictureBox picBox, ImageManager imgManager, ImageResizer imgResizer)
        {
            parentForm = form;
            pictureBox = picBox;
            imageManager = imgManager;
            imageResizer = imgResizer;
        }

        /// <summary>
        /// 画像サイズを変更する。
        /// 実際に画像ファイルをリサイズして保存し、
        /// PictureBoxの高さを更新し、ImageManagerの表示高さと
        /// テキストボックスの位置も連動して調整する。
        /// </summary>
        /// <param name="height">設定する高さ（px）</param>
        public void ResizeImage(int height)
        {
            if (!pictureBox.Visible) return;

            // 1. 元画像パスと古いリサイズ済みパスを取得
            string originalPath = imageManager.GetOriginalImagePath();
            string oldResizedPath = imageManager.GetResizedImagePath();

            if (string.IsNullOrEmpty(originalPath))
            {
                System.Diagnostics.Debug.WriteLine("元画像パスが設定されていません");
                return;
            }

            // 2. 古いリサイズ済み画像を削除
            if (!string.IsNullOrEmpty(oldResizedPath))
            {
                imageResizer.DeleteResizedImage(oldResizedPath);
            }

            // 3. 新しいサイズでリサイズ実行（ImageResizerに委譲）
            string newResizedPath = imageResizer.ResizeAndSaveImage(
                originalPath,
                height,
                parentForm.NoteId
            );

            if (newResizedPath != null)
            {
                System.Diagnostics.Debug.WriteLine($"リサイズ成功: {newResizedPath}");

                // 4. ImageManagerに新しいパスを設定
                imageManager.SetResizedImagePath(newResizedPath);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("リサイズをスキップしました（アニメーションGIFまたはエラー）");
            }

            // 5. 表示サイズを変更
            pictureBox.Height = height;
            imageManager.SetImageDisplayHeight(height);
            imageManager.AdjustTextBoxPosition();

            // テキスト領域が十分に見えるように付箋サイズを調整
            EnsureTextAreaVisible(height);

            // 6. 保存
            parentForm.SaveCurrentNoteState();
        }

        /// <summary>
        /// テキスト領域が十分に表示されるように付箋サイズを調整
        /// 画像サイズ変更時にテキストが見えなくならないようにする
        /// </summary>
        /// <param name="imageHeight">画像の高さ</param>
        private void EnsureTextAreaVisible(int imageHeight)
        {
            // タイトルバーの高さを取得
            int TitleBarHeight = parentForm.Controls["titleBar"]?.Height ?? 40;

            // 現在のテキスト領域の高さを計算
            int CurrentTextHeight = parentForm.ClientSize.Height - TitleBarHeight - imageHeight;

            // テキスト領域が最小高さより小さい場合、付箋を拡大
            if (CurrentTextHeight < MIN_TEXT_AREA_HEIGHT)
            {
                int RequiredHeight = TitleBarHeight + imageHeight + MIN_TEXT_AREA_HEIGHT;
                parentForm.ClientSize = new Size(parentForm.ClientSize.Width, RequiredHeight);

                // サイズ変更後、再度テキストボックスの位置を調整
                imageManager.AdjustTextBoxPosition();

                System.Diagnostics.Debug.WriteLine(
                    $"付箋サイズを自動調整: {parentForm.ClientSize.Height}px " +
                    $"(画像: {imageHeight}px, テキスト領域: {MIN_TEXT_AREA_HEIGHT}px確保)"
                );
            }
        }

        /// <summary>
        /// ユーザー指定場所に画像を保存する（表示サイズは変更しない）
        /// </summary>
        /// <param name="height">リサイズ後の高さ（px）、0の場合は元のサイズで保存</param>
        public void SaveResizedImageToUserLocation(int height)
        {
            string originalPath = imageManager.GetOriginalImagePath();

            if (string.IsNullOrEmpty(originalPath))
            {
                MessageBox.Show(
                    "保存する画像がありません。",
                    "情報",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // 推奨ファイル名
            string suggestedFileName = "image";

            if (height == 0)
            {
                // 元のサイズで保存（ImageResizerに委譲）
                imageResizer.SaveOriginalImageToUserLocation(originalPath, suggestedFileName);
            }
            else
            {
                // リサイズして保存（ImageResizerに委譲）
                imageResizer.ResizeAndSaveToUserLocation(originalPath, height, suggestedFileName);
            }
        }

        /// <summary>
        /// サイズ選択メニューの DropDownItems に項目を追加する。
        /// 呼び出し側で用意した ToolStripMenuItem（親メニュー）を渡す。
        /// </summary>
        /// <param name="parentMenuItem">「画像サイズ」サブメニューの親項目</param>
        public void CreateSizeMenuItems(ToolStripMenuItem parentMenuItem)
        {
            var items = new (string label, int size)[]
            {
                ("小 (100px)",   SIZE_SMALL),
                ("中 (150px)",   SIZE_MEDIUM),
                ("大 (200px)",   SIZE_LARGE),
                ("特大 (250px)", SIZE_EXTRA_LARGE),
            };

            foreach (var (label, size) in items)
            {
                var item = new ToolStripMenuItem(label);
                int capturedSize = size; // クロージャキャプチャ用
                item.Click += (s, e) => ResizeImage(capturedSize);
                parentMenuItem.DropDownItems.Add(item);
            }
        }
    }
}