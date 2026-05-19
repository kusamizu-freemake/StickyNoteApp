using System.Windows.Forms;

namespace StickyNoteApp
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // ------- コントロール宣言 -------
            this.grpFont = new System.Windows.Forms.GroupBox();
            this.lblFontFamily = new System.Windows.Forms.Label();
            this.txtFontFamily = new System.Windows.Forms.TextBox();
            this.btnFontSelect = new System.Windows.Forms.Button();
            this.lblFontSize = new System.Windows.Forms.Label();
            this.numFontSize = new System.Windows.Forms.NumericUpDown();
            this.grpPreview = new System.Windows.Forms.GroupBox();
            this.lblPreview = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            this.SuspendLayout();

            // ================================================================
            // フォーム
            // ================================================================
            this.Text = AppConstants.SettingsLabel.TITLE_SETTINGS;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ClientSize = new System.Drawing.Size(360, 280);
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.Font = new System.Drawing.Font("メイリオ", 9F);

            // ================================================================
            // グループ：フォント設定
            // ================================================================
            this.grpFont.Text = AppConstants.SettingsLabel.LABEL_FONT;
            this.grpFont.Location = new System.Drawing.Point(12, 12);
            this.grpFont.Size = new System.Drawing.Size(336, 110);
            this.grpFont.Font = new System.Drawing.Font("メイリオ", 9F);

            // フォントファミリーラベル
            this.lblFontFamily.Text = AppConstants.SettingsLabel.LABEL_FONT;
            this.lblFontFamily.Location = new System.Drawing.Point(12, 28);
            this.lblFontFamily.Size = new System.Drawing.Size(80, 22);
            this.lblFontFamily.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // フォントファミリーテキストボックス（読み取り専用：FontDialog で変更）
            this.txtFontFamily.Location = new System.Drawing.Point(96, 26);
            this.txtFontFamily.Size = new System.Drawing.Size(136, 24);
            this.txtFontFamily.ReadOnly = true;
            this.txtFontFamily.BackColor = System.Drawing.Color.White;
            this.txtFontFamily.TabStop = false;

            // フォント選択ボタン
            this.btnFontSelect.Text = AppConstants.SettingsLabel.BTN_FONT_SELECT;
            this.btnFontSelect.Location = new System.Drawing.Point(240, 24);
            this.btnFontSelect.Size = new System.Drawing.Size(84, 26);
            this.btnFontSelect.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFontSelect.Click += new System.EventHandler(this.BtnFontSelect_Click);

            // フォントサイズラベル
            this.lblFontSize.Text = AppConstants.SettingsLabel.LABEL_FONT_SIZE;
            this.lblFontSize.Location = new System.Drawing.Point(12, 66);
            this.lblFontSize.Size = new System.Drawing.Size(80, 22);
            this.lblFontSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // フォントサイズ NumericUpDown
            this.numFontSize.Location = new System.Drawing.Point(96, 64);
            this.numFontSize.Size = new System.Drawing.Size(70, 24);
            this.numFontSize.Minimum = (decimal)AppConstants.SettingsConfig.FONT_SIZE_MIN;
            this.numFontSize.Maximum = (decimal)AppConstants.SettingsConfig.FONT_SIZE_MAX;
            this.numFontSize.DecimalPlaces = 1;
            this.numFontSize.Increment = 0.5m;
            this.numFontSize.ValueChanged += new System.EventHandler(this.NumFontSize_ValueChanged);

            // グループにコントロールを追加
            this.grpFont.Controls.Add(this.lblFontFamily);
            this.grpFont.Controls.Add(this.txtFontFamily);
            this.grpFont.Controls.Add(this.btnFontSelect);
            this.grpFont.Controls.Add(this.lblFontSize);
            this.grpFont.Controls.Add(this.numFontSize);

            // ================================================================
            // グループ：プレビュー
            // ================================================================
            this.grpPreview.Text = AppConstants.SettingsLabel.LABEL_PREVIEW;
            this.grpPreview.Location = new System.Drawing.Point(12, 134);
            this.grpPreview.Size = new System.Drawing.Size(336, 80);

            this.lblPreview.Text = AppConstants.SettingsLabel.PREVIEW_TEXT;
            this.lblPreview.Location = new System.Drawing.Point(12, 28);
            this.lblPreview.Size = new System.Drawing.Size(310, 40);
            this.lblPreview.AutoSize = false;

            this.grpPreview.Controls.Add(this.lblPreview);

            // ================================================================
            // OK ボタン
            // ================================================================
            this.btnOk.Text = AppConstants.SettingsLabel.BTN_OK;
            this.btnOk.Location = new System.Drawing.Point(160, 234);
            this.btnOk.Size = new System.Drawing.Size(80, 30);
            this.btnOk.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);

            // ================================================================
            // キャンセルボタン
            // ================================================================
            this.btnCancel.Text = AppConstants.SettingsLabel.BTN_CANCEL;
            this.btnCancel.Location = new System.Drawing.Point(260, 234);
            this.btnCancel.Size = new System.Drawing.Size(90, 30);
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);

            // ================================================================
            // フォームにコントロール追加
            // ================================================================
            this.Controls.Add(this.grpFont);
            this.Controls.Add(this.grpPreview);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);

            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "SettingsForm";

            this.ResumeLayout(false);
        }

        #endregion

        // ------- フィールド宣言 -------
        private System.Windows.Forms.GroupBox grpFont;
        private System.Windows.Forms.Label lblFontFamily;
        private System.Windows.Forms.TextBox txtFontFamily;
        private System.Windows.Forms.Button btnFontSelect;
        private System.Windows.Forms.Label lblFontSize;
        private System.Windows.Forms.NumericUpDown numFontSize;
        private System.Windows.Forms.GroupBox grpPreview;
        private System.Windows.Forms.Label lblPreview;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}