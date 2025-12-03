using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// データベース整合性チェック・修正クラス
    /// </summary>
    public static class DatabaseIntegrityChecker
    {
        private static StringBuilder logBuilder = new StringBuilder();

        /// <summary>
        /// ログを追加
        /// </summary>
        private static void Log(string message)
        {
            string logMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            // デバッグ出力
            Debug.WriteLine(logMessage);
            // メッセージボックス用にログを蓄積
            logBuilder.AppendLine(message);
        }

        /// <summary>
        /// データベースの整合性をチェックし、問題を修正
        /// </summary>
        public static void CheckAndRepairSilent()
        {
            logBuilder.Clear();
            Log("=== データベース整合性チェック開始===");

            try
            {
                using (var con = new SqliteConnection(Database.GetConnectionString()))
                {
                    con.Open();

                    int totalRecords = GetTotalRecordCount(con);
                    int activeRecords = GetActiveRecordCount(con);
                    int deletedRecords = GetDeletedRecordCount(con);

                    Log($"レコード統計: 総数={totalRecords}, 有効={activeRecords}, 削除済み={deletedRecords}");

                    var emptyContentRecords = FindEmptyContentRecords(con);
                    Log($"空のContentを持つレコード: {emptyContentRecords.Count}件");

                    var duplicateIds = FindDuplicateIds(con);
                    if (duplicateIds.Count > 0)
                    {
                        Log($"警告: 重複ID発見: {duplicateIds.Count}件");
                    }

                    int purgedCount = PurgeOldDeletedRecords(con, days: 30);
                    if (purgedCount > 0)
                    {
                        Log($"物理削除されたレコード: {purgedCount}件");
                    }

                    int fixedCount = FixInvalidData(con);
                    if (fixedCount > 0)
                    {
                        Log($"修正されたレコード: {fixedCount}件");
                    }

                    OptimizeDatabase(con);
                    Log("✓ データベース最適化完了");
                }

                Log("=== データベース整合性チェック完了 ===");
            }
            catch (Exception ex)
            {
                Log($"❌ 整合性チェックエラー: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// データベースの整合性をチェックし、問題を修正（メッセージボックス表示）
        /// </summary>
        public static void CheckAndRepair()
        {
            logBuilder.Clear();
            Log("=== データベース整合性チェック開始 ===");
            Log("");

            try
            {
                using (var con = new SqliteConnection(Database.GetConnectionString()))
                {
                    con.Open();
                    // 1. 全レコード数の確認

                    int totalRecords = GetTotalRecordCount(con);
                    int activeRecords = GetActiveRecordCount(con);
                    int deletedRecords = GetDeletedRecordCount(con);

                    Log($"レコード統計:");
                    Log($"  総レコード数: {totalRecords}");
                    Log($"  有効なレコード: {activeRecords}");
                    Log($"  削除済みレコード: {deletedRecords}");
                    Log("");

                    // 2. 空のContentを持つレコードのチェック

                    var emptyContentRecords = FindEmptyContentRecords(con);
                    Log($"空のContentを持つレコード: {emptyContentRecords.Count}件");

                    // 3. 重複IDのチェック

                    var duplicateIds = FindDuplicateIds(con);
                    if (duplicateIds.Count > 0)
                    {
                        Log($"⚠️ 警告: 重複ID発見: {duplicateIds.Count}件");
                        foreach (var id in duplicateIds)
                        {
                            Log($"    - {id.Substring(0, 8)}...");
                        }
                    }
                    else
                    {
                        Log("✓ 重複IDなし");
                    }
                    Log("");

                    // 4. 古い削除済みレコードの物理削除

                    int purgedCount = PurgeOldDeletedRecords(con, days: 30);
                    if (purgedCount > 0)
                    {
                        Log($"物理削除されたレコード: {purgedCount}件");
                    }
                    else
                    {
                        Log("物理削除の必要なし");
                    }

                    // 5. 不正なデータの修正

                    int fixedCount = FixInvalidData(con);
                    if (fixedCount > 0)
                    {
                        Log($"修正されたレコード: {fixedCount}件");
                    }
                    else
                    {
                        Log("✓ 不正なデータなし");
                    }
                    Log("");

                    OptimizeDatabase(con);
                    Log("✓ データベース最適化完了");
                }

                Log("");
                Log("=== データベース整合性チェック完了 ===");

                // メッセージボックスで結果を表示

                MessageBox.Show(
                    logBuilder.ToString(),
                    "データベース整合性チェック結果",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                Log($"❌ 整合性チェックエラー: {ex.Message}");
                MessageBox.Show(
                    logBuilder.ToString() + $"\n\nエラー詳細:\n{ex.StackTrace}",
                    "整合性チェックエラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                throw;
            }
        }


        /// <summary>
        /// 全レコード数を取得
        /// </summary>
        private static int GetTotalRecordCount(SqliteConnection con)
        {
            string sql = "SELECT COUNT(*) FROM StickyNotes";
            using (var cmd = new SqliteCommand(sql, con))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// 有効なレコード数を取得
        /// </summary>
        private static int GetActiveRecordCount(SqliteConnection con)
        {
            string sql = "SELECT COUNT(*) FROM StickyNotes WHERE DeleteFlag = 0";
            using (var cmd = new SqliteCommand(sql, con))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// 削除済みレコード数を取得
        /// </summary>
        private static int GetDeletedRecordCount(SqliteConnection con)
        {
            string sql = "SELECT COUNT(*) FROM StickyNotes WHERE DeleteFlag = 1";
            using (var cmd = new SqliteCommand(sql, con))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// 空のContentを持つレコードを検索
        /// </summary>
        private static List<string> FindEmptyContentRecords(SqliteConnection con)
        {
            var records = new List<string>();
            string sql = "SELECT Id FROM StickyNotes WHERE (Content IS NULL OR Content = '') AND DeleteFlag = 0";

            using (var cmd = new SqliteCommand(sql, con))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    records.Add(reader["Id"].ToString());
                }
            }

            return records;
        }

        /// <summary>
        /// 重複IDを検索
        /// </summary>
        private static List<string> FindDuplicateIds(SqliteConnection con)
        {
            var duplicates = new List<string>();
            string sql = "SELECT Id, COUNT(*) as count FROM StickyNotes GROUP BY Id HAVING count > 1";

            using (var cmd = new SqliteCommand(sql, con))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    duplicates.Add(reader["Id"].ToString());
                }
            }

            return duplicates;
        }

        /// <summary>
        /// 古い削除済みレコードを物理削除
        /// </summary>
        private static int PurgeOldDeletedRecords(SqliteConnection con, int days)
        {
            string cutoffDate = DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd HH:mm:ss");
            string sql = @"
                DELETE FROM StickyNotes 
                WHERE DeleteFlag = 1 
                AND UpdatedAt < $CutoffDate
            ";

            using (var cmd = new SqliteCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("$CutoffDate", cutoffDate);
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 不正なデータを修正
        /// </summary
        private static int FixInvalidData(SqliteConnection con)
        {
            int fixedCount = 0;

            // NULL値の修正
            string sql = @"
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

            using (var cmd = new SqliteCommand(sql, con))
            {
                fixedCount += cmd.ExecuteNonQuery();
            }

            // 画面外の位置にある付箋を画面内に移動
            string sql2 = @"
                UPDATE StickyNotes 
                SET 
                    PosX = 100,
                    PosY = 100
                WHERE 
                    (PosX < -1000 OR PosX > 10000 OR
                     PosY < -1000 OR PosY > 10000)
                    AND DeleteFlag = 0
            ";

            using (var cmd = new SqliteCommand(sql2, con))
            {
                fixedCount += cmd.ExecuteNonQuery();
            }

            return fixedCount;
        }

        /// <summary>
        /// データベースを最適化（VACUUM）
        /// </summary>
        private static void OptimizeDatabase(SqliteConnection con)
        {
            using (var cmd = new SqliteCommand("VACUUM", con))
            {
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// データベースの詳細レポートを生成
        /// </summary>
        public static void GenerateReport()
        {
            logBuilder.Clear();
            Log("=== データベース詳細レポート ===");
            Log("");

            Debug.WriteLine("DatabaseIntegrityChecker.GenerateReport() 実行開始");

            try
            {
                using (var con = new SqliteConnection(Database.GetConnectionString()))
                {
                    con.Open();

                    // 全レコードの情報を出力
                    string sql = "SELECT * FROM StickyNotes ORDER BY CreatedAt ASC";
                    using (var cmd = new SqliteCommand(sql, con))
                    using (var reader = cmd.ExecuteReader())
                    {
                        int activeIndex = 1;
                        int deletedIndex = 1;

                        while (reader.Read())
                        {
                            string id = reader["Id"].ToString();
                            string content = reader["Content"].ToString();
                            int deleteFlag = Convert.ToInt32(reader["DeleteFlag"]);
                            int topMostFlag = Convert.ToInt32(reader["TopMostFlag"]);
                            string preview = string.IsNullOrEmpty(content) ? "(空)" :
                                (content.Length > 15 ? content.Substring(0, 15) + "..." : content);

                            if (deleteFlag == 0)
                            {
                                string topMost = topMostFlag == 1 ? "最前面" : "通常";
                                Log($"{activeIndex}. [有効] {preview} ({topMost})");
                                Log($"   ID: {id.Substring(0, 13)}...");
                                activeIndex++;
                            }
                            else
                            {
                                Log($"[削除{deletedIndex}] {preview}");
                                deletedIndex++;
                            }
                        }

                        if (activeIndex == 1)
                        {
                            Log("有効な付箋はありません");
                        }
                    }
                }

                Log("");
                Log("=== レポート終了 ===");

                Debug.WriteLine("レポート用メッセージボックスを表示します");

                // メッセージボックスで結果を表示
                MessageBox.Show(
                    logBuilder.ToString(),
                    "データベース詳細レポート",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                Debug.WriteLine("レポート用メッセージボックスが閉じられました");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"レポート生成エラー: {ex.Message}");
                Log($"❌ レポート生成エラー: {ex.Message}");
                MessageBox.Show(
                    logBuilder.ToString(),
                    "レポート生成エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}