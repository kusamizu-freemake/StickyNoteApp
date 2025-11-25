using System.Windows.Forms;

namespace StickyNoteApp
{
    partial class StickyNoteForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.titleBar = new System.Windows.Forms.Panel();
            this.btnClose = new System.Windows.Forms.Button();
            this.txtNote = new System.Windows.Forms.TextBox();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            this.SuspendLayout();

            // ------- フォーム -------
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.BackColor = System.Drawing.Color.Khaki;
            this.ClientSize = new System.Drawing.Size(260, 220);
            this.TopMost = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.ContextMenuStrip = this.contextMenu;

            // ------- タイトルバー -------
            this.titleBar.BackColor = System.Drawing.Color.Khaki;
            this.titleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleBar.Height = 40;
            this.titleBar.AutoSize = false;

            this.titleBar.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MoveForm_MouseDown);
            this.titleBar.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MoveForm_MouseMove);
            this.titleBar.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MoveForm_MouseUp);

            // ------- 閉じるボタン -------
            this.btnClose.Text = "✕";
            this.btnClose.Font = new System.Drawing.Font("Meiryo", 12F, System.Drawing.FontStyle.Bold);
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.BackColor = System.Drawing.Color.Transparent;
            this.btnClose.ForeColor = System.Drawing.Color.Black;
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Right; // 右端に配置
            this.btnClose.Size = new System.Drawing.Size(30, 30);
            this.btnClose.TabStop = false;
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.Margin = new Padding(0);
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

            // ------- テキストボックス -------
            this.txtNote.Multiline = true;
            this.txtNote.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtNote.Font = new System.Drawing.Font("Meiryo", 11F);
            this.txtNote.BackColor = System.Drawing.Color.Khaki;
            this.txtNote.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtNote.AcceptsTab = true;
            this.txtNote.WordWrap = true;

            // ------- 右クリック削除メニュー -------
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteMenuItem});
            this.deleteMenuItem.Text = "削除";
            this.deleteMenuItem.Click += new System.EventHandler(this.deleteMenuItem_Click);

            // ------- コントロール追加 -------
            this.titleBar.Controls.Add(this.btnClose);
            this.Controls.Add(this.txtNote);
            this.Controls.Add(this.titleBar);

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "StickyNoteForm";
            this.Text = "";

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Panel titleBar;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.TextBox txtNote;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem deleteMenuItem;
    }
}