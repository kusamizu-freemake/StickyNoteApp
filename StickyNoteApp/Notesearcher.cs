using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace StickyNoteApp
{

    ///<summary>
    /// 付箋の単語検索ロジッククラス
    /// Database.LoadAll()で取得したStickyNoteDataをアプリ側でフィルタリングする
    ///</summary>
    public static class NoteSearcher
    {
        ///<summary>
        /// 検索オプションにしたが会って全付箋を検索し、一致した付箋の一覧を返す
        /// </summary>
        public static List<SearchResultItem> Search(SearchOptions Options)
        {
            var Results = new List<SearchResultItem>();

            try
            {
                // DBから有効な付箋データ（DeleteFlag = 0)を全権取得
                List<StickyNoteData> AllNotes = Database.LoadAll();

                foreach (StickyNoteData Note in AllNotes)
                {
                    if (IsMatch(Note.Content, Options))
                    {
                        Results.Add(new SearchResultItem
                        {
                            NoteId = Note.Id,
                            Preview = Note.Content,
                            CreatedAt = Note.CreatedAt
                        });
                    }
                }

                Debug.WriteLine(string.Format(
                    AppConstants.SearchMsg.MSG_RESULT_COUNT, Results.Count));
            }

            catch (Exception Ex)
            {
                Debug.WriteLine(string.Format(
                    AppConstants.SearchMsg.MSG_SEARCH_ERROR, Ex.Message));

                // 呼び出し元（SearchForm）へ例外を伝播させず、空リストを返す
                // → 検索中のエラーで UI がクラッシュするのを防ぐ
            }

            return Results;
        }

        // =============================================
        //  内部ロジック
        // =============================================

        ///<summary>
        /// 1件の付箋本文が検索オプションに一致するか判定する
        /// </summary>
        private static bool IsMatch(string Content, SearchOptions Options)
        {
            // 本文が空の付箋は検索対象外
            if (string.IsNullOrEmpty(Content)) return false;

            // 大小文字の区別設定に応じて比較対象を正規化
            StringComparison Comparison = Options.MatchCase
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            if (Options.WholeWord)
            {
                // 完全一致：本文全体がキーワードと一致するか
                return string.Equals(Content.Trim(), Options.Keyword, Comparison);
            }
            else
            {
                // 部分一致：本文がキーワードが含まれるか
                return Content.IndexOf(Options.Keyword, Comparison) >= 0;
            }
        }
    }
}
