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
        private static StringBuilder LogBuilder = new StringBuilder();

        /// <summary>
        /// ログを追加
        /// </summary>
        private static void Log(string Message)
        {
            string LogMessage = $"[{DateTime.Now.ToString(AppConstants.IntegrityConfig.LOG_TIME_FORMAT)}] {Message}";
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
                Cmd.CommandText = string.Format(AppConstants.IntegrityConfig.SQL_BUSY_TIMEOUT, AppConstants.IntegrityConfig.BUSY_TIMEOUT_MS);
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
            Log(AppConstants.IntegrityMsg.MSG_CHECK_START);

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

                Log(string.Format(AppConstants.IntegrityMsg.MSG_RECORD_STATS, TotalRecords, ActiveRecords, DeletedRecords));

                List<string> EmptyContentRecords;
                using (var Con = new SqliteConnection(Database.GetConnectionString()))
                {
                    Con.Open();
                    EmptyContentRecords = FindEmptyContentRecords(Con);
                } // ← 接続を閉じる

                Log(string.Format(AppConstants.IntegrityMsg.MSG_EMPTY_CONTENT, EmptyContentRecords.Count));

                List<string> DuplicateIds;
                using (var Con = new SqliteConnection(Database.GetConnectionString()))
                {
                    Con.Open();
                    DuplicateIds = FindDuplicateIds(Con);
                } // ← 接続を閉じる

                if (DuplicateIds.Count > 0)
                {
                    Log(string.Format(AppConstants.IntegrityMsg.MSG_DUPLICATE_FOUND, DuplicateIds.Count));
                }

                int PurgedCount;
                using (var Con = new SqliteConnection(Database.GetConnectionString()))
                {
                    Con.Open();
                    PurgedCount = PurgeOldDeletedRecords(Con, Days: AppConstants.IntegrityConfig.PURGE_DAYS);
                } // ← 接続を閉じる

                if (PurgedCount > 0)
                {
                    Log(string.Format(AppConstants.IntegrityMsg.MSG_PURGED_COUNT, PurgedCount));
                }

                int FixedCount;
                using (var Con = new SqliteConnection(Database.GetConnectionString()))
                {
                    Con.Open();
                    FixedCount = FixInvalidData(Con);
                } // ← 接続を閉じる

                if (FixedCount > 0)
                {
                    Log(string.Format(AppConstants.IntegrityMsg.MSG_FIXED_COUNT, FixedCount));
                }

                Log(AppConstants.IntegrityMsg.MSG_OPTIMIZE_COMPLETE);
                Log(AppConstants.IntegrityMsg.MSG_CHECK_COMPLETE);
            }
            catch (Exception Ex)
            {
                Log(string.Format(AppConstants.IntegrityMsg.MSG_INTEGRITY_ERROR, Ex.Message));
                throw;
            }
        }

        /// <summary>
        /// データベースの整合性をチェックし、結果をダイアログで表示
        /// </summary>
        public static void CheckAndRepair()
        {
            LogBuilder.Clear();
            Log(AppConstants.IntegrityMsg.MSG_CHECK_START_VERBOSE);
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

                Log(AppConstants.IntegrityMsg.MSG_RECORD_STATS_LABEL);
                Log(string.Format(AppConstants.IntegrityMsg.MSG_TOTAL_RECORDS, TotalRecords));
                Log(string.Format(AppConstants.IntegrityMsg.MSG_ACTIVE_RECORDS, ActiveRecords));
                Log(string.Format(AppConstants.IntegrityMsg.MSG_DELETED_RECORDS, DeletedRecords));
                Log("");

                List<string> EmptyContentRecords;
                using (var Con = OpenConnectionWithTimeout())
                {
                    EmptyContentRecords = FindEmptyContentRecords(Con);
                }

                Log(string.Format(AppConstants.IntegrityMsg.MSG_EMPTY_CONTENT, EmptyContentRecords.Count));

                List<string> DuplicateIds;
                using (var Con = OpenConnectionWithTimeout())
                {
                    DuplicateIds = FindDuplicateIds(Con);
                }

                if (DuplicateIds.Count > 0)
                {
                    Log(string.Format(AppConstants.IntegrityMsg.MSG_DUPLICATE_FOUND_EMOJI, DuplicateIds.Count));
                    foreach (var Id in DuplicateIds)
                    {
                        Log(string.Format(AppConstants.IntegrityMsg.MSG_DUPLICATE_ID_ENTRY, Id.Substring(0, AppConstants.IntegrityConfig.ID_SHORT_PREVIEW_LENGTH)));
                    }
                }
                else
                {
                    Log(AppConstants.IntegrityMsg.MSG_NO_DUPLICATE);
                }
                Log("");

                // PurgeOldDeletedRecords を実行（リトライ機能付き）
                int PurgedCount = 0;
                int RetryCount = 0;

                while (RetryCount < AppConstants.IntegrityConfig.MAX_RETRIES)
                {
                    try
                    {
                        using (var Con = OpenConnectionWithTimeout())
                        {
                            PurgedCount = PurgeOldDeletedRecords(Con, Days: AppConstants.IntegrityConfig.PURGE_DAYS);
                        }
                        break; // 成功したらループを抜ける
                    }
                    catch (SqliteException Ex) when (Ex.SqliteErrorCode == AppConstants.IntegrityConfig.SQLITE_ERROR_BUSY)
                    {
                        RetryCount++;
                        if (RetryCount >= AppConstants.IntegrityConfig.MAX_RETRIES)
                        {
                            Log(AppConstants.IntegrityMsg.MSG_SKIP_PURGE_BUSY);
                            PurgedCount = 0;
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(AppConstants.IntegrityConfig.RETRY_WAIT_MS);
                        }
                    }
                }

                if (PurgedCount > 0)
                {
                    Log(string.Format(AppConstants.IntegrityMsg.MSG_PURGED_COUNT, PurgedCount));
                }
                else
                {
                    Log(AppConstants.IntegrityMsg.MSG_NO_PURGE);
                }

                int FixedCount;
                using (var Con = OpenConnectionWithTimeout())
                {
                    FixedCount = FixInvalidData(Con);
                }

                if (FixedCount > 0)
                {
                    Log(string.Format(AppConstants.IntegrityMsg.MSG_FIXED_COUNT, FixedCount));
                }
                else
                {
                    Log(AppConstants.IntegrityMsg.MSG_NO_INVALID_DATA);
                }
                Log("");

                Log(AppConstants.IntegrityMsg.MSG_OPTIMIZE_COMPLETE);
                Log("");
                Log(AppConstants.IntegrityMsg.MSG_CHECK_COMPLETE);

                MessageBox.Show(
                    LogBuilder.ToString(),
                    AppConstants.IntegrityTitle.TITLE_INTEGRITY_RESULT,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception Ex)
            {
                Log(string.Format(AppConstants.IntegrityMsg.MSG_INTEGRITY_ERROR, Ex.Message));
                MessageBox.Show(
                    LogBuilder.ToString() + $"\n\nエラー詳細:\n{Ex.StackTrace}",
                    AppConstants.IntegrityTitle.TITLE_INTEGRITY_ERROR,
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
            string CutoffDate = DateTime.Now.AddDays(-Days).ToString(AppConstants.SharedConfig.DATE_TIME_FORMAT);
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
            Log(AppConstants.IntegrityMsg.MSG_REPORT_START);
            Log("");

            Debug.WriteLine(AppConstants.IntegrityMsg.MSG_REPORT_EXE_START);

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
                            string Preview = string.IsNullOrEmpty(Content) ? AppConstants.IntegrityMsg.MSG_EMPTY_CONTENT2 :
                                (Content.Length > AppConstants.IntegrityConfig.PREVIEW_MAX_LENGTH ? Content.Substring(0, AppConstants.IntegrityConfig.PREVIEW_MAX_LENGTH) + "..." : Content);

                            if (DeleteFlag == 0)
                            {
                                string TopMostLabel = TopMostFlag == 1 ? AppConstants.IntegrityMsg.MSG_TOP_MOST : AppConstants.IntegrityMsg.MSG_NORMAL;
                                Log(string.Format(AppConstants.IntegrityMsg.MSG_ACTIVE_NOTE, ActiveIndex, Preview, TopMostLabel));
                                Log(string.Format(AppConstants.IntegrityMsg.MSG_ACTIVE_NOTE_ID, Id.Substring(0, AppConstants.IntegrityConfig.ID_PREVIEW_LENGTH)));
                                ActiveIndex++;
                            }
                            else
                            {
                                Log(string.Format(AppConstants.IntegrityMsg.MSG_DELETED_NOTE, DeletedIndex, Preview));
                                DeletedIndex++;
                            }
                        }

                        if (ActiveIndex == 1)
                        {
                            Log(AppConstants.IntegrityMsg.MSG_NO_ACTIVE_NOTES);
                        }
                    }
                } // ← 接続を閉じる

                Log("");
                Log(AppConstants.IntegrityMsg.MSG_REPORT_END);

                MessageBox.Show(
                    LogBuilder.ToString(),
                    AppConstants.IntegrityTitle.TITLE_REPORT_DETAIL,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(string.Format(AppConstants.IntegrityMsg.MSG_REPORT_ERROR_DEBUG, Ex.Message));
                Log(string.Format(AppConstants.IntegrityMsg.MSG_REPORT_ERROR, Ex.Message));
                MessageBox.Show(
                    LogBuilder.ToString(),
                    AppConstants.IntegrityTitle.TITLE_REPORT_ERROR,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}