using System;
using System.Diagnostics;

namespace StickyNoteApp
{
    /// <summary>
    /// アプリケーション全体で共有される設定や状態を管理するクラス
    /// </summary>
    public static class Common
    {
        // 復元処理中かどうか
        public static bool IsRestoring { get; private set; } = true;

        // DB保存を許可してよいか
        public static bool IsSaveEnabled { get; private set; } = false;

        // 画像データ移行中かどうか
        public static bool IsImageMigrating { get; private set; } = false;

        public static bool CanSaveDatabase =>
            IsSaveEnabled && !IsImageMigrating;

        /// <summary>
        /// 復元処理の開始を記録
        /// </summary>
        public static void BeginRestore()
        {
            IsRestoring = true;
            IsSaveEnabled = false;
            System.Diagnostics.Debug.WriteLine("[Common] 復元処理開始");
        }

        /// <summary>
        /// 復元処理の完了を記録
        /// </summary>
        public static void EndRestore()
        {
            IsRestoring = false;
            // ここではまだ保存を有効にしない
            System.Diagnostics.Debug.WriteLine("[Common] 復元処理完了（保存はまだ無効）");
        }
    }
}
