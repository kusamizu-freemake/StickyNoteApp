using System;
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

        // 依存オブジェクト
        private readonly StickyNoteForm parentForm;
        private readonly PictureBox pictureBox;
        private readonly ImageManager imageManager;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="form">親付箋フォーム（保存メソッドの呼び出し元）</param>
        /// <param name="picBox">画像を表示しているPictureBox</param>
        /// <param name="imgManager">画像マネージャー（状態更新・レイアウト調整の委譲先）</param>
        public ImageResizeManager(StickyNoteForm form, PictureBox picBox, ImageManager imgManager)
        {
            parentForm = form;
            pictureBox = picBox;
            imageManager = imgManager;
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

            // 実際の画像ファイルをリサイズして保存
            string resizedPath = imageManager.CreateResizedImage(height);

            if (resizedPath != null)
            {
                System.Diagnostics.Debug.WriteLine($"リサイズ成功: {resizedPath}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("リサイズをスキップしました（アニメーションGIFまたはエラー）");
            }

            // 既存の処理（表示サイズの変更）
            pictureBox.Height = height;
            imageManager.SetImageDisplayHeight(height);
            imageManager.AdjustTextBoxPosition();

            parentForm.SaveCurrentNoteState();
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