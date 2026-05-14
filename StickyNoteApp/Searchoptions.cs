using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StickyNoteApp
{
   ///<summary>
   /// 検索オプションを格納するデータクラス
   /// SearchFormからNoteSearcherへ検索条件を渡す為に使用する
   /// </summary>
   public class SearchOptions
    {
        // 検索キーワード
        public string Keyword { get; set; }

        // 大小文字を区別して検索するか
        public bool MatchCase { get; set; }

        // 完全一致(キーワード全体と一致）で検索するか
        public bool WholeWord { get; set; }
    }
}
