using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace StickyNoteApp
{
    ///<summary>
    ///付箋の単語検索ダイアログ
    ///</summary>
    public partial class SearchForm : Form
    {
        // 検索結果リスト(ListBoxのインデックスと対応)
        private List<SearchResultItem> SearchResults = new List<SearchResultItem>();

        ///<summary>
        /// 付箋フォーカス用コールバック(TrayManagerFormから注入)
        /// 引数:NoteId
        ///</summary>
        public Action<string> FocusNoteCallback { get; set; }

        public SearchForm()
        {
            InitializeComponent();
            InitializeKeyEvents();
        }

        // ========================================
        /// 初期化
        // ========================================

        /// <summary>
        /// キーボードショートカットの初期化
        /// Enter → 検索実行 / Escape → ダイアログを閉じる
        /// </summary>
        private void InitializeKeyEvents()
        {
            /// フォーム全体のキーイベント
            this.KeyDown += (Sender, Args) =>
            {
                if (Args.KeyCode == Keys.Escape) this.Close(); // 
                if (Args.KeyCode == Keys.Enter) ExecuteSearch(); // 
            };

            // ComboBox内のEnterキー(ピープ音を抑制)
            CmbSearchWord.KeyDown += (Sender, Args) =>
            {
                if (Args.KeyCode == Keys.Enter)
                {
                    ExecuteSearch();
                    Args.SuppressKeyPress = true;
                }
            };
        }

        // ========================================
        /// イベントハンドラー
        // ========================================

        // <summary>
        // 検索ボタンクリック
        // </summary>
        private void BtnSearch_Click(object Sender, EventArgs Args)
        {
            ExecuteSearch();
        }

        // <summary>
        // 閉じるボタンクリック
        // </summary>
        private void BtnClose_Click(object Sender, EventArgs Args)
        {
            this.Close();
        }

        // <summary>
        // 結果一覧ダブルクリック→該当付箋にフォーカス
        // </summary>
        private void LstResults_DoubleClick(object Sender, EventArgs Args)
        {
            FocusSelectedNote();
        }

        // =============================================
        //  検索処理
        // =============================================

        // <summary>
        // 検索を実行し、結果を ListBox に表示する
        // <summary>
        private void ExecuteSearch()
        {
            string Keyword = CmbSearchWord.Text.Trim();

            // 空文字チェック
            if (string.IsNullOrEmpty(Keyword))
            {
                MessageBox.Show(
                    AppConstants.SearchMsg.MSG_EMPTY_KEYWORD,
                    AppConstants.SharedTitle.INFO,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // 検索オプションを設定
            SearchOptions Options = new SearchOptions
            {
                Keyword = Keyword,
                MatchCase = ChkMatchCase.Checked,
                WholeWord = ChkWholeWord.Checked,
            };

            // 検索実行
            SearchResults = NoteSearcher.Search(Options);

            // 入力ワードを履歴に追加
            AddSearchHistory(Keyword);

            // 結果をListBoxへ反映
            UpdateResultList();

        }

        /// <summary>
        /// 検索結果をListBoxに反映する
        /// </summary>
        private void UpdateResultList()
        {
            LstResults.Items.Clear();

            if (SearchResults.Count == 0)
            {
                LblResultCount.Text = AppConstants.SearchMsg.MSG_NO_RESULTS;
                LstResults.Items.Add(AppConstants.SearchMsg.MSG_NO_RESULTS_ITEM);
                return;
            }

            LblResultCount.Text = string.Format(
                AppConstants.SearchMsg.MSG_RESULT_COUNT, SearchResults.Count);

            foreach (SearchResultItem Item in SearchResults)
            {
                LstResults.Items.Add(FormatResultItem(Item));
            }
        }

        /// <summary>
        /// 結果アイテムの表示文字列を生成する
        /// 例：「2025-05-01 12:34 | Hello World...」
        /// </summary>
        private string FormatResultItem(SearchResultItem Item)
        {
            string Date = Item.CreatedAt.Length >= AppConstants.SearchConfig.DATE_DISPLAY_LENGTH
                ? Item.CreatedAt.Substring(0, AppConstants.SearchConfig.DATE_DISPLAY_LENGTH)
                : Item.CreatedAt;

            string Preview = Item.Preview.Length > AppConstants.SearchConfig.PREVIEW_MAX_LENGTH
                ? Item.Preview.Substring(0, AppConstants.SearchConfig.PREVIEW_MAX_LENGTH)
                + AppConstants.SearchConfig.PREVIEW_ELLIPSIS
                : Item.Preview;

            return string.Format(AppConstants.SearchConfig.RESULT_FORMAT, Date, Preview);
        }

        ///<summary>
        /// 選択中の結果付箋をフォーカスする
        ///</summary>
        private void FocusSelectedNote()
        {
            int SelectedIndex = LstResults.SelectedIndex;
            if (SelectedIndex < 0 || SelectedIndex >= SearchResults.Count) return;

            SearchResultItem Selected = SearchResults[SelectedIndex];
            FocusNoteCallback?.Invoke(Selected.NoteId);

        }

        ///<summary>
        /// 検索ワードの履歴をComboBoxに追加する
        /// 既存と重複する場合は削除して先頭に追加し、上限を超えた分は末尾から削除する
        ///</summary>
        private void AddSearchHistory(string Keyword)
        {
            if (CmbSearchWord.Items.Contains(Keyword))
                CmbSearchWord.Items.Remove(Keyword);

            CmbSearchWord.Items.Insert(0, Keyword);

            while (CmbSearchWord.Items.Count > AppConstants.SearchConfig.HISTORY_MAX_COUNT)
                CmbSearchWord.Items.RemoveAt(CmbSearchWord.Items.Count - 1);

            CmbSearchWord.SelectedIndex = 0;
        }
    }
}