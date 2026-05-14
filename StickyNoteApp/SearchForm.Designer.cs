using System.Windows.Forms;

namespace StickyNoteApp
{
    partial class SearchForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer Components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (Components != null))
            {
                Components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        private void InitializeComponent()
        {
            LblSearchWord = new System.Windows.Forms.Label();
            CmbSearchWord = new System.Windows.Forms.ComboBox();
            BtnSearch = new System.Windows.Forms.Button();
            BtnClose = new System.Windows.Forms.Button();
            ChkMatchCase = new System.Windows.Forms.CheckBox();
            ChkWholeWord = new System.Windows.Forms.CheckBox();
            GrpResults = new System.Windows.Forms.GroupBox();
            LstResults = new System.Windows.Forms.ListBox();
            LblResultCount = new System.Windows.Forms.Label();
            GrpResults.SuspendLayout();
            this.SuspendLayout();

            // -------  フォーム  -------
            this.Text = AppConstants.SearchLabel.FORM_TITLE;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ClientSize = new System.Drawing.Size(460, 380);
            this.Font = new System.Drawing.Font("Meiryo", 9F);
            this.KeyPreview = true;

            // -------  検索文字(N): ラベル  -------
            LblSearchWord.Text = AppConstants.SearchLabel.LABEL_SEARCH_WORD;
            LblSearchWord.Location = new System.Drawing.Point(12, 16);
            LblSearchWord.Size = new System.Drawing.Size(90, 20);
            LblSearchWord.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // -------  検索文字 コンボボックス  -------
            CmbSearchWord.Location = new System.Drawing.Point(108, 13);
            CmbSearchWord.Size = new System.Drawing.Size(224, 24);
            CmbSearchWord.MaxDropDownItems = AppConstants.SearchConfig.HISTORY_MAX_COUNT;
            CmbSearchWord.TabIndex = 0;

            // -------  検索(F) ボタン  -------
            BtnSearch.Text = AppConstants.SearchLabel.BTN_SEARCH;
            BtnSearch.Location = new System.Drawing.Point(344, 12);
            BtnSearch.Size = new System.Drawing.Size(100, 26);
            BtnSearch.TabIndex = 1;
            BtnSearch.Click += new System.EventHandler(this.BtnSearch_Click);

            // -------  閉じる ボタン  -------
            BtnClose.Text = AppConstants.SearchLabel.BTN_CLOSE;
            BtnClose.Location = new System.Drawing.Point(344, 44);
            BtnClose.Size = new System.Drawing.Size(100, 26);
            BtnClose.TabIndex = 2;
            BtnClose.Click += new System.EventHandler(this.BtnClose_Click);

            // -------  大小文字の区別(C)  -------
            ChkMatchCase.Text = AppConstants.SearchLabel.CHK_MATCH_CASE;
            ChkMatchCase.Location = new System.Drawing.Point(12, 48);
            ChkMatchCase.Size = new System.Drawing.Size(160, 20);
            ChkMatchCase.TabIndex = 3;

            // -------  完全一致(E)  -------
            ChkWholeWord.Text = AppConstants.SearchLabel.CHK_WHOLE_WORD;
            ChkWholeWord.Location = new System.Drawing.Point(12, 72);
            ChkWholeWord.Size = new System.Drawing.Size(160, 20);
            ChkWholeWord.TabIndex = 4;

            // -------  件数ラベル  -------
            LblResultCount.Text = string.Empty;
            LblResultCount.Location = new System.Drawing.Point(12, 100);
            LblResultCount.Size = new System.Drawing.Size(430, 18);
            LblResultCount.ForeColor = System.Drawing.Color.Gray;

            // -------  検索結果 グループボックス  -------
            GrpResults.Text = AppConstants.SearchLabel.GRP_RESULTS;
            GrpResults.Location = new System.Drawing.Point(12, 122);
            GrpResults.Size = new System.Drawing.Size(432, 240);
            GrpResults.TabStop = false;
            GrpResults.Controls.Add(LstResults);

            // -------  結果一覧 ListBox  -------
            LstResults.Dock = System.Windows.Forms.DockStyle.Fill;
            LstResults.BorderStyle = System.Windows.Forms.BorderStyle.None;
            LstResults.Font = new System.Drawing.Font("Meiryo", 9F);
            LstResults.ItemHeight = 18;
            LstResults.TabIndex = 5;
            LstResults.DoubleClick += new System.EventHandler(this.LstResults_DoubleClick);

            // -------  コントロール追加  -------
            this.Controls.Add(LblSearchWord);
            this.Controls.Add(CmbSearchWord);
            this.Controls.Add(BtnSearch);
            this.Controls.Add(BtnClose);
            this.Controls.Add(ChkMatchCase);
            this.Controls.Add(ChkWholeWord);
            this.Controls.Add(LblResultCount);
            this.Controls.Add(GrpResults);

            GrpResults.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label LblSearchWord;
        private System.Windows.Forms.ComboBox CmbSearchWord;
        private System.Windows.Forms.Button BtnSearch;
        private System.Windows.Forms.Button BtnClose;
        private System.Windows.Forms.CheckBox ChkMatchCase;
        private System.Windows.Forms.CheckBox ChkWholeWord;
        private System.Windows.Forms.GroupBox GrpResults;
        private System.Windows.Forms.ListBox LstResults;
        private System.Windows.Forms.Label LblResultCount;
    }
}