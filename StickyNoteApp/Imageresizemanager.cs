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
                Debug.WriteLine(AppConstants.ImageResizeManagerMsg.MSG_NO_ORIGINAL_PATH);
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
                Debug.WriteLine(string.Format(AppConstants.ImageResizeManagerMsg.MSG_RESIZE_SUCCESS, NewResizedPath));

                // 4. ImageManagerに新しいパスを設定
                ImageManager.SetResizedImagePath(NewResizedPath);
            }
            else
            {
                Debug.WriteLine(AppConstants.ImageResizeManagerMsg.MSG_RESIZE_SKIPPED);
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
            int TitleBarHeight = ParentForm.Controls[AppConstants.SharedImage.CONTROL_TITLE_BAR]?.Height ?? AppConstants.SharedImage.DEFAULT_TITLE_BAR_HEIGHT;

            // 現在のテキスト領域の高さを計算
            int CurrentTextHeight = ParentForm.ClientSize.Height - TitleBarHeight - ImageHeight;

            // テキスト領域が最小高さより小さい場合、付箋を拡大
            if (CurrentTextHeight < AppConstants.SharedImage.MIN_TEXT_AREA_HEIGHT)
            {
                int RequiredHeight = TitleBarHeight + ImageHeight + AppConstants.SharedImage.MIN_TEXT_AREA_HEIGHT;
                ParentForm.ClientSize = new Size(ParentForm.ClientSize.Width, RequiredHeight);

                // サイズ変更後、再度テキストボックスの位置を調整
                ImageManager.AdjustTextBoxPosition();

                Debug.WriteLine(string.Format(AppConstants.ImageResizeManagerMsg.MSG_AUTO_ADJUST,
                    ParentForm.ClientSize.Height, ImageHeight, AppConstants.SharedImage.MIN_TEXT_AREA_HEIGHT));
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
                MessageBox.Show(AppConstants.ImageResizeManagerMsg.MSG_NO_IMAGE_TO_SAVE, AppConstants.SharedTitle.INFO, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                (AppConstants.ImageResizeManagerConfig.MENU_LABEL_SMALL,       AppConstants.ImageResizeManagerConfig.SIZE_SMALL),
                (AppConstants.ImageResizeManagerConfig.MENU_LABEL_MEDIUM,      AppConstants.ImageResizeManagerConfig.SIZE_MEDIUM),
                (AppConstants.ImageResizeManagerConfig.MENU_LABEL_LARGE,       AppConstants.ImageResizeManagerConfig.SIZE_LARGE),
                (AppConstants.ImageResizeManagerConfig.MENU_LABEL_EXTRA_LARGE, AppConstants.ImageResizeManagerConfig.SIZE_EXTRA_LARGE),
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