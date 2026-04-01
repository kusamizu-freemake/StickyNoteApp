using System;
using System.Diagnostics;

namespace StickyNoteApp
{
    /// <summary>
    /// アプリケーション全体で共有される設定や状態を管理するクラス
    /// </summary>
    public static class Common
    {
        // ログメッセージ定数
        private const string MSG_BEGIN_RESTORE = "[Common] 復元処理開始";
        private const string MSG_END_RESTORE = "[Common] 復元処理完了（保存はまだ無効）";
        private const string MSG_ENABLE_SAVE = "[Common] データベース保存を有効化";
        private const string MSG_BEGIN_IMAGE_MIGRATE = "[Common] 画像データ移行開始";
        private const string MSG_END_IMAGE_MIGRATE = "[Common] 画像データ移行完了";

        // 復元処理中かどうか
        public static bool IsRestoring { get; private set; } = true;

        // DB保存を許可してよいか
        public static bool IsSaveEnabled { get; private set; } = false;

        // 画像データ移行中かどうか
        public static bool IsImageMigrating { get; private set; } = false;

        // データベース保存が可能な状態かどうか
        public static bool CanSaveDatabase =>
            IsSaveEnabled && !IsImageMigrating;

        /// <summary>
        /// 復元処理の開始を記録
        /// </summary>
        public static void BeginRestore()
        {
            IsRestoring = true;
            IsSaveEnabled = false;
            Debug.WriteLine(MSG_BEGIN_RESTORE);
        }

        /// <summary>
        /// 復元処理の完了を記録
        /// </summary>
        public static void EndRestore()
        {
            IsRestoring = false;
            // ここではまだ保存を有効にしない
            Debug.WriteLine(MSG_END_RESTORE);
        }

        /// <summary>
        /// データベース保存を有効化
        /// </summary>
        public static void EnableSave()
        {
            IsSaveEnabled = true;
            Debug.WriteLine(MSG_ENABLE_SAVE);
        }

        /// <summary>
        /// 画像データ移行処理の開始を記録
        /// </summary>
        public static void BeginImageMigration()
        {
            IsImageMigrating = true;
            IsSaveEnabled = false;
            Debug.WriteLine(MSG_BEGIN_IMAGE_MIGRATE);
        }

        /// <summary>
        /// 画像データ移行処理の完了を記録
        /// </summary>
        public static void EndImageMigration()
        {
            IsImageMigrating = false;
            IsSaveEnabled = true;
            Debug.WriteLine(MSG_END_IMAGE_MIGRATE);
        }
    }
}