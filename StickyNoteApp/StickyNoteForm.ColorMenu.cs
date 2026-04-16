using System;
using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// 付箋ウィンドウ - 色変更メニュー
    /// 色変更の共通処理・各色メニュークリックイベント
    /// </summary>
    public partial class StickyNoteForm : Form
    {
        /// <summary>
        /// 色を変更する共通処理。
        /// BackColorChanged イベントを一時解除してから色を適用し、
        /// 完了後に保存してイベントを再登録する。
        /// </summary>
        /// <param name="color">適用する背景色</param>
        private void ChangeColor(Color color)
        {
            this.BackColorChanged -= BackColor_Changed;
            this.txtNote.BackColor = color;
            this.BackColor = color;
            this.titleBar.BackColor = Color.WhiteSmoke;
            this.BackColorChanged += BackColor_Changed;
            SaveCurrentNoteState();
        }

        /// <summary>色メニュー：イエロー</summary>
        private void SelectColor_Yellow_Click(object sender, EventArgs e) => ChangeColor(Color.Khaki);

        /// <summary>色メニュー：ピンク</summary>
        private void SelectColor_Pink_Click(object sender, EventArgs e) => ChangeColor(Color.LightPink);

        /// <summary>色メニュー：ブルー</summary>
        private void SelectColor_Blue_Click(object sender, EventArgs e) => ChangeColor(Color.LightBlue);

        /// <summary>色メニュー：グリーン</summary>
        private void SelectColor_Green_Click(object sender, EventArgs e) => ChangeColor(Color.LightGreen);

        /// <summary>色メニュー：オレンジ</summary>
        private void SelectColor_Orange_Click(object sender, EventArgs e) => ChangeColor(Color.LightSalmon);

        /// <summary>色メニュー：パープル</summary>
        private void SelectColor_Purple_Click(object sender, EventArgs e) => ChangeColor(Color.Plum);
    }
}
