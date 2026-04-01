using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// データベース整合性チェック・修正クラス
    /// </summary>
    public static class DatabaseIntegrityChecker
    {
        // ログメッセージ定数
        private const string MSG_CHECK_START = "=== データベース整合性チェック開始===";
        private const string MSG_CHECK_START_VERBOSE = "=== データベース整合性チェック開始 ===";
        private const string MSG_CHECK_COMPLETE = "=== データベース整合性チェック完了 ===";
        private const string MSG_OPTIMIZE_COMPLETE = "✓ データベース最適化完了";
        private const string MSG_RECORD_STATS = "レコード統計: 総数={0}, 有効={1}, 削除済み={2}";
        private const string MSG_RECORD_STATS_LABEL = "レコード統計:";
        private const string MSG_TOTAL_RECORDS = "  総レコード数: {0}";
        private const string MSG_ACTIVE_RECORDS = "  有効なレコード: {0}";
        private const string MSG_DELETED_RECORDS = "  削除済みレコード: {0}";
        private const string MSG_EMPTY_CONTENT = "空のContentを持つレコード: {0}件";
        private const string MSG_DUPLICATE_FOUND = "警告: 重複ID発見: {0}件";
        private const string MSG_DUPLICATE_FOUND_EMOJI = "⚠️ 警告: 重複ID発見: {0}件";
        private const string MSG_NO_DUPLICATE = "✓ 重複IDなし";
        private const string MSG_PURGED_COUNT = "物理削除されたレコード: {0}件";
        private const string MSG_NO_PURGE = "物理削除の必要なし";
        private const string MSG_SKIP_PURGE_BUSY = "警告: 古いレコードの削除をスキップしました（データベースビジー）";
        private const string MSG_FIXED_COUNT = "修正されたレコード: {0}件";
        private const string MSG_NO_INVALID_DATA = "✓ 不正なデータなし";
        private const string MSG_INTEGRITY_ERROR = "❌ 整合性チェックエラー: {0}";
        private const string MSG_REPORT_START = "=== データベース詳細レポート ===";
        private const string MSG_REPORT_END = "=== レポート終了 ===";
        private const string MSG_REPORT_EXE_START = "DatabaseIntegrityChecker.GenerateReport() 実行開始";
        private const string MSG_REPORT_ERROR = "❌ レポート生成エラー: {0}";
        private const string MSG_REPORT_ERROR_DEBUG = "レポート生成エラー: {0}";
        private const string MSG_NO_ACTIVE_NOTES = "有効な付箋はありません";
        private const string MSG_ACTIVE_NOTE = "{0}. [有効] {1} ({2})";
        private const string MSG_ACTIVE_NOTE_ID = "   ID: {0}...";
        private const string MSG_DELETED_NOTE = "[削除{0}] {1}";
        private const string MSG_TOP_MOST = "最前面";
        private const string MSG_NORMAL = "通常";
        private const string MSG_EMPTY_CONTENT2 = "(空)";
        private const string MSG_DuplicateIdEntry = "    - {0}...";

        // ダイアログタイトル定数
        private const string TITLE_INTEGRITY_RESULT = "データベース整合性チェック結果";
        private const string TITLE_INTEGRITY_ERROR = "整合性チェックエラー";
        private const string TITLE_REPORT_DETAIL = "データベース詳細レポート";
        private const string TITLE_REPORT_ERROR = "レポート生成エラー";

        // 数値定数
        private const int BUSY_TIMEOUT_MS = 10000;
        private const int PURGE_DAYS = 30;
        private const int MAX_RETRIES = 3;
        private const int RETRY_WAIT_MS = 1000;
        private const int SQLITE_ERROR_BUSY = 5;
        private const int PREVIEW_MAX_LENGTH = 15;
        private const int ID_PREVIEW_LENGTH = 13;
        private const int ID_SHORT_PREVIEW_LENGTH = 8;

        // SQLクエリ定数
        private const string SQL_BUSY_TIMEOUT = "PRAGMA busy_timeout = {0};";

        // 日時フォーマット
        private const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
        private const string LOG_TIME_FORMAT = "HH:mm:ss";

        private static StringBuilder LogBuilder = new StringBuilder();

        /// <summary>
        /// ログを追加
        /// </summary>
        private static void Log(string Message)
        {
            string LogMessage = $"[{DateTime.Now.ToString(LOG_TIME_FORMAT)}] {Message}";
            // デバッグ出力
            Debug.WriteLine(LogMessage);
            // メッセージボックス用にログを蓄積
            LogBuilder.AppendLine(Message);
        }

        /// <summary>
        /// 接続を開き、busy_timeout を設定して返す（共通処理）
        /// </summary>
        private static SqliteConnection OpenConnectionWithTimeout()
        {
            var Con = new SqliteConnection(Database.GetConnectionString());
            Con.Open();
            using (var Cmd = Con.CreateCommand())
            {
                Cmd.CommandText = string.Format(SQL_BUSY_TIMEOUT, BUSY_TIMEOUT_MS);
                Cmd.ExecuteNonQuery();
            }
            return Con;
        }

        /// <summary>
        /// データベースの整合性をチェックし、問題を修正
        /// </summary>
        public static void CheckAndRepairSilent()
        {
            LogBuilder.Clear();
            Log(MSG_CHECK_START);

            try
            {
                // 各処理ごとに接続を開閉

                int TotalRecords, ActiveRecords, DeletedRecords;
                using (var Con = new SqliteConnection(Database.GetConnectionString()))
                {
                    Con.Open();
                    TotalRecords = GetTotalRecordCount(Con);
                    ActiveRecords = GetActiveRecordCount(Con);
                    DeletedRecords = GetDeletedRecordCount(Con);
                } // ← 接続を閉じる

                Log(string.Format(MSG_RECORD_STATS, TotalRecords, ActiveRecords, DeletedRecords));

                List<string> EmptyContentRecords;
                using (var Con = new SqliteConnection(Database.GetConnectionString()))
                {
                    Con.Open();
                    EmptyContentRecords = FindEmptyContentRecords(Con);
                } // ← 接続を閉じる

                Log(string.Format(MSG_EMPTY_CONTENT, EmptyContentRecords.Count));

                List<string> DuplicateIds;
                using (var Con = new SqliteConnection(Database.GetConnectionString()))
                {
                    Con.Open();
                    DuplicateIds = FindDuplicateIds(Con);
                } // ← 接続を閉じる

                if (DuplicateIds.Count > 0)
                {
                    Log(string.Format(MSG_DUPLICATE_FOUND, DuplicateIds.Count));
                }

                int PurgedCount;
                using (var Con = new SqliteConnection(Database.GetConnectionString()))
                {
                    Con.Open();
                    PurgedCount = PurgeOldDeletedRecords(Con, Days: PURGE_DAYS);
                } // ← 接続を閉じる

                if (PurgedCount > 0)
                {
                    Log(string.Format(MSG_PURGED_COUNT, PurgedCount));
                }

                int FixedCount;
                using (var Con = new SqliteConnection(Database.GetConnectionString()))
                {
                    Con.Open();
                    FixedCount = FixInvalidData(Con);
                } // ← 接続を閉じる

                if (FixedCount > 0)
                {
                    Log(string.Format(MSG_FIXED_COUNT, FixedCount));
                }

                Log(MSG_OPTIMIZE_COMPLETE);
                Log(MSG_CHECK_COMPLETE);
            }
            catch (Exception Ex)
            {
                Log(string.Format(MSG_INTEGRITY_ERROR, Ex.Message));
                throw;
            }
        }

        /// <summary>
        /// データベースの整合性をチェックし、結果をダイアログで表示
        /// </summary>
        public static void CheckAndRepair()
        {
            LogBuilder.Clear();
            Log(MSG_CHECK_START_VERBOSE);
            Log("");

            try
            {
                int TotalRecords, ActiveRecords, DeletedRecords;
                using (var Con = OpenConnectionWithTimeout())
                {
                    TotalRecords = GetTotalRecordCount(Con);
                    ActiveRecords = GetActiveRecordCount(Con);
                    DeletedRecords = GetDeletedRecordCount(Con);
                }

                Log(MSG_RECORD_STATS_LABEL);
                Log(string.Format(MSG_TOTAL_RECORDS, TotalRecords));
                Log(string.Format(MSG_ACTIVE_RECORDS, ActiveRecords));
                Log(string.Format(MSG_DELETED_RECORDS, DeletedRecords));
                Log("");

                List<string> EmptyContentRecords;
                using (var Con = OpenConnectionWithTimeout())
                {
                    EmptyContentRecords = FindEmptyContentRecords(Con);
                }

                Log(string.Format(MSG_EMPTY_CONTENT, EmptyContentRecords.Count));

                List<string> DuplicateIds;
                using (var Con = OpenConnectionWithTimeout())
                {
                    DuplicateIds = FindDuplicateIds(Con);
                }

                if (DuplicateIds.Count > 0)
                {
                    Log(string.Format(MSG_DUPLICATE_FOUND_EMOJI, DuplicateIds.Count));
                    foreach (var Id in DuplicateIds)
                    {
                        Log(string.Format(MSG_DuplicateIdEntry, Id.Substring(0, ID_SHORT_PREVIEW_LENGTH)));
                    }
                }
                else
                {
                    Log(MSG_NO_DUPLICATE);
                }
                Log("");

                // PurgeOldDeletedRecords を実行（リトライ機能付き）
                int PurgedCount = 0;
                int RetryCount = 0;

                while (RetryCount < MAX_RETRIES)
                {
                    try
                    {
                        using (var Con = OpenConnectionWithTimeout())
                        {
                            PurgedCount = PurgeOldDeletedRecords(Con, Days: PURGE_DAYS);
                        }
                        break; // 成功したらループを抜ける
                    }
                    catch (SqliteException Ex) when (Ex.SqliteErrorCode == SQLITE_ERROR_BUSY)
                    {
                        RetryCount++;
                        if (RetryCount >= MAX_RETRIES)
                        {
                            Log(MSG_SKIP_PURGE_BUSY);
                            PurgedCount = 0;
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(RETRY_WAIT_MS);
                        }
                    }
                }

                if (PurgedCount > 0)
                {
                    Log(string.Format(MSG_PURGED_COUNT, PurgedCount));
                }
                else
                {
                    Log(MSG_NO_PURGE);
                }

                int FixedCount;
                using (var Con = OpenConnectionWithTimeout())
                {
                    FixedCount = FixInvalidData(Con);
                }

                if (FixedCount > 0)
                {
                    Log(string.Format(MSG_FIXED_COUNT, FixedCount));
                }
                else
                {
                    Log(MSG_NO_INVALID_DATA);
                }
                Log("");

                Log(MSG_OPTIMIZE_COMPLETE);
                Log("");
                Log(MSG_CHECK_COMPLETE);

                MessageBox.Show(
                    LogBuilder.ToString(),
                    TITLE_INTEGRITY_RESULT,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception Ex)
            {
                Log(string.Format(MSG_INTEGRITY_ERROR, Ex.Message));
                MessageBox.Show(
                    LogBuilder.ToString() + $"\n\nエラー詳細:\n{Ex.StackTrace}",
                    TITLE_INTEGRITY_ERROR,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                throw;
            }
        }

        /// <summary>
        /// 全レコード数を取得
        /// </summary>
        private static int GetTotalRecordCount(SqliteConnection Con)
        {
            string Sql = "SELECT COUNT(*) FROM StickyNotes";
            using (var Cmd = new SqliteCommand(Sql, Con))
            {
                return Convert.ToInt32(Cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// 有効なレコード数を取得
        /// </summary>
        private static int GetActiveRecordCount(SqliteConnection Con)
        {
            string Sql = "SELECT COUNT(*) FROM StickyNotes WHERE DeleteFlag = 0";
            using (var Cmd = new SqliteCommand(Sql, Con))
            {
                return Convert.ToInt32(Cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// 削除済みレコード数を取得
        /// </summary>
        private static int GetDeletedRecordCount(SqliteConnection Con)
        {
            string Sql = "SELECT COUNT(*) FROM StickyNotes WHERE DeleteFlag = 1";
            using (var Cmd = new SqliteCommand(Sql, Con))
            {
                return Convert.ToInt32(Cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// 空のContentを持つレコードを検索
        /// </summary>
        private static List<string> FindEmptyContentRecords(SqliteConnection Con)
        {
            var Records = new List<string>();
            string Sql = "SELECT Id FROM StickyNotes WHERE (Content IS NULL OR Content = '') AND DeleteFlag = 0";

            using (var Cmd = new SqliteCommand(Sql, Con))
            using (var Reader = Cmd.ExecuteReader())
            {
                while (Reader.Read())
                {
                    Records.Add(Reader["Id"].ToString());
                }
            }

            return Records;
        }

        /// <summary>
        /// 重複IDを検索
        /// </summary>
        private static List<string> FindDuplicateIds(SqliteConnection Con)
        {
            var Duplicates = new List<string>();
            string Sql = "SELECT Id, COUNT(*) as count FROM StickyNotes GROUP BY Id HAVING count > 1";

            using (var Cmd = new SqliteCommand(Sql, Con))
            using (var Reader = Cmd.ExecuteReader())
            {
                while (Reader.Read())
                {
                    Duplicates.Add(Reader["Id"].ToString());
                }
            }

            return Duplicates;
        }

        /// <summary>
        /// 古い削除済みレコードを物理削除
        /// </summary>
        private static int PurgeOldDeletedRecords(SqliteConnection Con, int Days)
        {
            string CutoffDate = DateTime.Now.AddDays(-Days).ToString(DATE_TIME_FORMAT);
            string Sql = @"
                DELETE FROM StickyNotes 
                WHERE DeleteFlag = 1 
                AND UpdatedAt < $CutoffDate
            ";

            using (var Cmd = new SqliteCommand(Sql, Con))
            {
                Cmd.Parameters.AddWithValue("$CutoffDate", CutoffDate);
                return Cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 不正なデータを修正
        /// </summary>
        private static int FixInvalidData(SqliteConnection Con)
        {
            int FixedCount = 0;

            // NULL値の修正
            string Sql = @"
                UPDATE StickyNotes 
                SET 
                    Content = COALESCE(Content, ''),
                    PosX = COALESCE(PosX, 100),
                    PosY = COALESCE(PosY, 100),
                    Width = COALESCE(Width, 260),
                    Height = COALESCE(Height, 220),
                    BgR = COALESCE(BgR, 240),
                    BgG = COALESCE(BgG, 230),
                    BgB = COALESCE(BgB, 140),
                    TopMostFlag = COALESCE(TopMostFlag, 0),
                    DeleteFlag = COALESCE(DeleteFlag, 0)
                WHERE 
                    Content IS NULL OR
                    PosX IS NULL OR
                    PosY IS NULL OR
                    Width IS NULL OR
                    Height IS NULL OR
                    BgR IS NULL OR
                    BgG IS NULL OR
                    BgB IS NULL OR
                    TopMostFlag IS NULL OR
                    DeleteFlag IS NULL
            ";

            using (var Cmd = new SqliteCommand(Sql, Con))
            {
                FixedCount += Cmd.ExecuteNonQuery();
            }

            // 画面外の位置にある付箋を画面内に移動
            string Sql2 = @"
                UPDATE StickyNotes 
                SET 
                    PosX = 100,
                    PosY = 100
                WHERE 
                    (PosX < -1000 OR PosX > 10000 OR
                     PosY < -1000 OR PosY > 10000)
                    AND DeleteFlag = 0
            ";

            using (var Cmd = new SqliteCommand(Sql2, Con))
            {
                FixedCount += Cmd.ExecuteNonQuery();
            }

            return FixedCount;
        }

        /// <summary>
        /// データベース詳細レポートを生成しダイアログで表示
        /// </summary>
        public static void GenerateReport()
        {
            LogBuilder.Clear();
            Log(MSG_REPORT_START);
            Log("");

            Debug.WriteLine(MSG_REPORT_EXE_START);

            try
            {
                using (var Con = new SqliteConnection(Database.GetConnectionString()))
                {
                    Con.Open();

                    string Sql = "SELECT * FROM StickyNotes ORDER BY CreatedAt ASC";
                    using (var Cmd = new SqliteCommand(Sql, Con))
                    using (var Reader = Cmd.ExecuteReader())
                    {
                        int ActiveIndex = 1;
                        int DeletedIndex = 1;

                        while (Reader.Read())
                        {
                            string Id = Reader["Id"].ToString();
                            string Content = Reader["Content"].ToString();
                            int DeleteFlag = Convert.ToInt32(Reader["DeleteFlag"]);
                            int TopMostFlag = Convert.ToInt32(Reader["TopMostFlag"]);
                            string Preview = string.IsNullOrEmpty(Content) ? MSG_EMPTY_CONTENT2 :
                                (Content.Length > PREVIEW_MAX_LENGTH ? Content.Substring(0, PREVIEW_MAX_LENGTH) + "..." : Content);

                            if (DeleteFlag == 0)
                            {
                                string TopMostLabel = TopMostFlag == 1 ? MSG_TOP_MOST : MSG_NORMAL;
                                Log(string.Format(MSG_ACTIVE_NOTE, ActiveIndex, Preview, TopMostLabel));
                                Log(string.Format(MSG_ACTIVE_NOTE_ID, Id.Substring(0, ID_PREVIEW_LENGTH)));
                                ActiveIndex++;
                            }
                            else
                            {
                                Log(string.Format(MSG_DELETED_NOTE, DeletedIndex, Preview));
                                DeletedIndex++;
                            }
                        }

                        if (ActiveIndex == 1)
                        {
                            Log(MSG_NO_ACTIVE_NOTES);
                        }
                    }
                } // ← 接続を閉じる

                Log("");
                Log(MSG_REPORT_END);

                MessageBox.Show(
                    LogBuilder.ToString(),
                    TITLE_REPORT_DETAIL,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(string.Format(MSG_REPORT_ERROR_DEBUG, Ex.Message));
                Log(string.Format(MSG_REPORT_ERROR, Ex.Message));
                MessageBox.Show(
                    LogBuilder.ToString(),
                    TITLE_REPORT_ERROR,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}