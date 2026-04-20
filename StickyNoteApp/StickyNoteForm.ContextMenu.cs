using System;
using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// 付箋ウィンドウ - 右クリックメニュー制御
    /// ContextMenu_Opening / Button_Close_Click / StickyNoteMenu_*_Click
    /// </summary>
    public partial class StickyNoteForm : Form
    {
        /// <summary>
        /// 閉じるボタンクリック時の処理
        /// </summary>
        private void Button_Close_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    AppConstants.StickyNoteFormMsg.MSG_CONFIRM_DELETE,
                    AppConstants.SharedTitle.CONFIRM,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Database.SoftDelete(NoteId);
                this.Close();
            }
        }

        /// <summary>
        /// 右クリックメニュー：新しい付箋を作成
        /// </summary>
        private void StickyNoteMenu_New_Click(object sender, EventArgs e)
        {
            StickyNoteForm newNote = new StickyNoteForm();
            // 現在の付箋の近くに表示
            newNote.Location = new Point(
                this.Left + AppConstants.StickyNoteFormConfig.NEW_NOTE_OFFSET_X,
                this.Top  + AppConstants.StickyNoteFormConfig.NEW_NOTE_OFFSET_Y);
            newNote.Show();
        }

        /// <summary>
        /// 右クリックメニュー：最前面表示の切り替え
        /// </summary>
        private void StickyNoteMenu_TopMost_Click(object sender, EventArgs e)
        {
            this.TopMost = topMostMenuItem.Checked;
            // 最前面表示切り替え完了時に保存
            SaveCurrentNoteState();
        }

        /// <summary>
        /// 右クリックメニュー：付箋削除処理
        /// </summary>
        private void StickyNoteMenu_Delete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    AppConstants.StickyNoteFormMsg.MSG_CONFIRM_DELETE,
                    AppConstants.SharedTitle.CONFIRM,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Database.SoftDelete(NoteId);
                this.Close();
            }
        }

        /// <summary>
        /// 右クリックメニューが開く前に、画像の有無に応じて
        /// 削除・サイズ変更メニューを表示 / 非表示にする
        /// </summary>
        private void ContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 画像があるかどうかをチェック
            bool hasImage = pictureBox != null && pictureBox.Image != null;
            // 画像がある時だけ表示する（削除・サイズ変更）
            removeImageMenuItem.Visible = hasImage;
            imageSizeMenuItem.Visible   = hasImage;
        }
    }
}
