using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// 付箋ウィンドウ
    /// PictureBox 初期化・画像 UI
    /// </summary>
    public partial class StickyNoteForm
    {
        /// <summary>
        /// 付箋に貼り付けた画像を表示する PictureBox の初期化
        /// </summary>
        private void InitializePictureBox()
        {
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.Dock = DockStyle.Top;
            pictureBox.Height = 0;
            pictureBox.Visible = false;
            pictureBox.BackColor = Color.White;
            pictureBox.BorderStyle = BorderStyle.FixedSingle;

            // ダブルクリックで画像を削除
            pictureBox.DoubleClick += PictureBox_DoubleClick;

            // リサイズ用のマウスイベントハンドラーを追加 → StickyNoteForm.Resize.cs
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseUp += PictureBox_MouseUp;

            // 右クリックメニューを追加
            var imageContextMenu = new ContextMenuStrip();

            // 画像を編集（置き換え）
            var inserteditImageItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_IMAGE_EDIT);
            inserteditImageItem.Click += (s, e) => imageManager?.SelectAndLoadImageFromFile();
            imageContextMenu.Items.Add(inserteditImageItem);
            imageContextMenu.Items.Add(new ToolStripSeparator()); // 区切り線

            // 画像を削除
            var deleteImageItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_IMAGE_DELETE);
            deleteImageItem.Click += (s, e) => imageManager?.RemoveImage();
            imageContextMenu.Items.Add(deleteImageItem);
            imageContextMenu.Items.Add(new ToolStripSeparator()); // 区切り線

            // 画像サイズ変更サブメニュー
            var resizeImageItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_IMAGE_SIZE);
            imageResizeManager.CreateSizeMenuItems(resizeImageItem);
            imageContextMenu.Items.Add(resizeImageItem);

            // 名前を付けて保存サブメニュー
            var saveAsImageItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_IMAGE_SAVE_AS);

            var saveAsSmallItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_SAVE_SMALL);
            saveAsSmallItem.Click += (s, e) => imageResizeManager?.SaveResizedImageToUserLocation(AppConstants.ImageResizeManagerConfig.SIZE_SMALL);
            saveAsImageItem.DropDownItems.Add(saveAsSmallItem);

            var saveAsMediumItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_SAVE_MEDIUM);
            saveAsMediumItem.Click += (s, e) => imageResizeManager?.SaveResizedImageToUserLocation(AppConstants.ImageResizeManagerConfig.SIZE_MEDIUM);
            saveAsImageItem.DropDownItems.Add(saveAsMediumItem);

            var saveAsLargeItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_SAVE_LARGE);
            saveAsLargeItem.Click += (s, e) => imageResizeManager?.SaveResizedImageToUserLocation(AppConstants.ImageResizeManagerConfig.SIZE_LARGE);
            saveAsImageItem.DropDownItems.Add(saveAsLargeItem);

            var saveAsExtraLargeItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_SAVE_EXTRA_LARGE);
            saveAsExtraLargeItem.Click += (s, e) => imageResizeManager?.SaveResizedImageToUserLocation(AppConstants.ImageResizeManagerConfig.SIZE_EXTRA_LARGE);
            saveAsImageItem.DropDownItems.Add(saveAsExtraLargeItem);

            saveAsImageItem.DropDownItems.Add(new ToolStripSeparator()); // 区切り線

            var saveAsOriginalItem = new ToolStripMenuItem(AppConstants.StickyNoteFormLabel.MENU_SAVE_ORIGINAL);
            saveAsOriginalItem.Click += (s, e) => imageResizeManager?.SaveResizedImageToUserLocation(0); // 0 は元サイズ
            saveAsImageItem.DropDownItems.Add(saveAsOriginalItem);

            imageContextMenu.Items.Add(saveAsImageItem);

            pictureBox.ContextMenuStrip = imageContextMenu;

            // txtNote の前に追加（タイトルバーの下）
            this.Controls.Add(pictureBox);
            // pictureBox を txtNote より前（上）に移動
            this.Controls.SetChildIndex(pictureBox, 0);
        }

        /// <summary>
        /// PictureBox ダブルクリック時の処理（画像削除確認）
        /// </summary>
        private void PictureBox_DoubleClick(object sender, System.EventArgs e)
        {
            var result = MessageBox.Show(
                AppConstants.StickyNoteFormMsg.MSG_CONFIRM_REMOVE_IMAGE,
                AppConstants.SharedTitle.CONFIRM,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                imageManager?.RemoveImage();
            }
        }

        /// <summary>
        /// 右クリックメニュー：画像を挿入・編集（画像を再選択して貼り換える）
        /// </summary>
        private void InsertEditImageMenuItem_Click(object sender, System.EventArgs e)
        {
            // ImageManager の既存メソッドを呼び出すだけでOK
            // このメソッドは内部で「古い画像を削除 → 新しい画像を保存」を自動実行
            imageManager?.SelectAndLoadImageFromFile();
        }

        /// <summary>
        /// 右クリックメニュー：画像を削除
        /// </summary>
        private void RemoveImageMenuItem_Click(object sender, System.EventArgs e)
        {
            if (pictureBox?.Image != null)
            {
                var result = MessageBox.Show(
                    AppConstants.StickyNoteFormMsg.MSG_CONFIRM_REMOVE_IMAGE,
                    AppConstants.SharedTitle.CONFIRM,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    imageManager?.RemoveImage();
                }
            }
        }
    }
}
