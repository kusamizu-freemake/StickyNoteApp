using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StickyNoteApp
{
    ///<summary>
    /// 検索結果の1件分を表すデータクラス
    /// NoteSearcherからSeachFormへ結果を渡すために使用する
    /// </summary>
    public class SearchResultItem
    {
        ///<summary>
        /// 付箋の一意ID
        /// フォーカス処理(TryManagerForm)で使用する
        /// </summary>
        public string NoteId { get; set; }

        ///<summary>
        /// 付箋の本文テキスト
        /// SearchForm側でPREVIEW_MAX_LENGTHに切り詰めて表示する
        /// </summary>
        public string Preview { get; set; }

        ///<summary>
        /// 付箋の作成日時文字列("yyyy-MM-dd HH:mm:ss"形式）
        /// StickyNoteData.CreatedAtをそのまま格納する
        /// </summary>
        public string CreatedAt { get; set; }
    }
    
}
