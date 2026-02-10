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
            this.newNoteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.topMostMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.colorMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.colorYellowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.colorPinkMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.colorBlueMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.colorGreenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.colorOrangeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.colorPurpleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.inserteditImageMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeImageMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageSizeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageSmallMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageMediumMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageLargeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageExtraLargeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reminderMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reminder5MinMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reminder10MinMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reminder30MinMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reminder60MinMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reminderCancelMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separatorMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.deleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            this.SuspendLayout();

            // ------- フォーム -------
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.BackColor = System.Drawing.Color.Khaki;
            this.ClientSize = new System.Drawing.Size(260, 220);
            this.TopMost = false; // ← デフォルトをfalseに変更（最前面表示の問題を改善）
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.ContextMenuStrip = this.contextMenu;

            // ------- タイトルバー -------
            this.titleBar.BackColor = System.Drawing.Color.WhiteSmoke; // ← 白系の固定色に変更
            this.titleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleBar.Height = 40;
            this.titleBar.AutoSize = false;
            // タイトルバーのマウスイベントにフォーム移動処理を割り当て
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
            this.btnClose.Click += new System.EventHandler(this.Button_Close_Click);

            // ------- テキストボックス -------
            this.txtNote.Multiline = true;
            this.txtNote.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtNote.Font = new System.Drawing.Font("Meiryo", 11F);
            this.txtNote.BackColor = System.Drawing.Color.Khaki;
            this.txtNote.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtNote.AcceptsTab = true;
            this.txtNote.WordWrap = true;
            this.txtNote.Leave += new System.EventHandler(this.StickyNoteForm_Deactivate);  // ←Deactivateに変更

            // ------- 右クリックメニュー -------
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.newNoteMenuItem,
                this.topMostMenuItem,
                this.colorMenuItem,
                this.imageMenuItem,
                this.reminderMenuItem,
                this.separatorMenuItem1,
                this.deleteMenuItem
            });

            // メニューが開く前に画像の有無をチェックして表示/非表示を制御
            this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ContextMenu_Opening);

            // 新しい付箋を作成
            this.newNoteMenuItem.Text = "新しい付箋を作成";
            this.newNoteMenuItem.Click += new System.EventHandler(this.StickyNoteMenu_New_Click);

            // 最前面表示の切り替え
            this.topMostMenuItem.Text = "最前面に表示";
            this.topMostMenuItem.CheckOnClick = true;
            this.topMostMenuItem.Checked = false; // ← デフォルトをfalseに変更
            this.topMostMenuItem.Click += new System.EventHandler(this.StickyNoteMenu_TopMost_Click);

            // 色の変更（サブメニュー）
            this.colorMenuItem.Text = "色の変更";
            this.colorMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.colorYellowMenuItem,
                this.colorPinkMenuItem,
                this.colorBlueMenuItem,
                this.colorGreenMenuItem,
                this.colorOrangeMenuItem,
                this.colorPurpleMenuItem
            });

            // 各色のメニュー項目
            this.colorYellowMenuItem.Text = "イエロー";
            this.colorYellowMenuItem.Click += new System.EventHandler(this.SelectColor_Yellow_Click);

            this.colorPinkMenuItem.Text = "ピンク";
            this.colorPinkMenuItem.Click += new System.EventHandler(this.SelectColor_Pink_Click);

            this.colorBlueMenuItem.Text = "ブルー";
            this.colorBlueMenuItem.Click += new System.EventHandler(this.SelectColor_Blue_Click);

            this.colorGreenMenuItem.Text = "グリーン";
            this.colorGreenMenuItem.Click += new System.EventHandler(this.SelectColor_Green_Click);

            this.colorOrangeMenuItem.Text = "オレンジ";
            this.colorOrangeMenuItem.Click += new System.EventHandler(this.SelectColor_Orange_Click);

            this.colorPurpleMenuItem.Text = "パープル";
            this.colorPurpleMenuItem.Click += new System.EventHandler(this.SelectColor_Purple_Click);

            // 画像メニュー（サブメニュー）
            this.imageMenuItem.Text = "画像";
            this.imageMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.inserteditImageMenuItem,
                new System.Windows.Forms.ToolStripSeparator(),
                this.imageSizeMenuItem,
                new System.Windows.Forms.ToolStripSeparator(),
                this.removeImageMenuItem
            });

            // 画像を挿入・編集（置き換え）
            this.inserteditImageMenuItem.Text = "画像を挿入・編集（置き換え）";
            this.inserteditImageMenuItem.Click += new System.EventHandler(this.InsertEditImageMenuItem_Click);

            // 画像を削除
            this.removeImageMenuItem.Text = "画像を削除";
            this.removeImageMenuItem.Click += new System.EventHandler(this.RemoveImageMenuItem_Click);

            // 画像サイズ変更（サブメニュー）
            this.imageSizeMenuItem.Text = "画像サイズ変更";
            this.imageSizeMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.imageSmallMenuItem,
                this.imageMediumMenuItem,
                this.imageLargeMenuItem,
                this.imageExtraLargeMenuItem
            });

            // 画像サイズ：小
            this.imageSmallMenuItem.Text = "小 (100px)";
            //this.imageSmallMenuItem.Click += new System.EventHandler(this.ImageSmallMenuItem_Click);

            // 画像サイズ：中
            this.imageMediumMenuItem.Text = "中 (150px)";
            //this.imageMediumMenuItem.Click += new System.EventHandler(this.ImageMediumMenuItem_Click);

            // 画像サイズ：大
            this.imageLargeMenuItem.Text = "大 (200px)";
            //this.imageLargeMenuItem.Click += new System.EventHandler(this.ImageLargeMenuItem_Click);

            // 画像サイズ：特大
            this.imageExtraLargeMenuItem.Text = "特大 (250px)";
            //this.imageExtraLargeMenuItem.Click += new System.EventHandler(this.ImageExtraLargeMenuItem_Click);

            // リマインダー（サブメニュー）
            this.reminderMenuItem.Text = "リマインダー";
            this.reminderMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.reminder5MinMenuItem,
                this.reminder10MinMenuItem,
                this.reminder30MinMenuItem,
                this.reminder60MinMenuItem,
                new System.Windows.Forms.ToolStripSeparator(),
                this.reminderCancelMenuItem
            });

            // リマインダーの各メニュー項目
            this.reminder5MinMenuItem.Text = "時間を指定...";
            this.reminder5MinMenuItem.Click += new System.EventHandler(this.ReminderCustomMenuItem_Click);

            this.reminder10MinMenuItem.Text = "10分後";
            this.reminder10MinMenuItem.Click += new System.EventHandler(this.Reminder10MinMenuItem_Click);

            this.reminder30MinMenuItem.Text = "30分後";
            this.reminder30MinMenuItem.Click += new System.EventHandler(this.Reminder30MinMenuItem_Click);

            this.reminder60MinMenuItem.Text = "60分後";
            this.reminder60MinMenuItem.Click += new System.EventHandler(this.Reminder60MinMenuItem_Click);

            this.reminderCancelMenuItem.Text = "リマインダーをキャンセル";
            this.reminderCancelMenuItem.Click += new System.EventHandler(this.ReminderCancelMenuItem_Click);

            // 区切り線
            this.separatorMenuItem1.Name = "separatorMenuItem1";

            // 削除
            this.deleteMenuItem.Text = "削除";
            this.deleteMenuItem.Click += new System.EventHandler(this.StickyNoteMenu_Delete_Click);

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
        public System.Windows.Forms.TextBox txtNote; // ← publicに変更
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem newNoteMenuItem;
        private System.Windows.Forms.ToolStripMenuItem topMostMenuItem;
        private System.Windows.Forms.ToolStripMenuItem colorMenuItem;
        private System.Windows.Forms.ToolStripMenuItem colorYellowMenuItem;
        private System.Windows.Forms.ToolStripMenuItem colorPinkMenuItem;
        private System.Windows.Forms.ToolStripMenuItem colorBlueMenuItem;
        private System.Windows.Forms.ToolStripMenuItem colorGreenMenuItem;
        private System.Windows.Forms.ToolStripMenuItem colorOrangeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem colorPurpleMenuItem;
        private System.Windows.Forms.ToolStripMenuItem imageMenuItem;
        private System.Windows.Forms.ToolStripMenuItem inserteditImageMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeImageMenuItem;
        private System.Windows.Forms.ToolStripMenuItem imageSizeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem imageSmallMenuItem;
        private System.Windows.Forms.ToolStripMenuItem imageMediumMenuItem;
        private System.Windows.Forms.ToolStripMenuItem imageLargeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem imageExtraLargeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reminderMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reminder5MinMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reminder10MinMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reminder30MinMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reminder60MinMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reminderCancelMenuItem;
        private System.Windows.Forms.ToolStripSeparator separatorMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem deleteMenuItem;
    }
}