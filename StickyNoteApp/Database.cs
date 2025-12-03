using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace StickyNoteApp
{
    /// <summary>
    /// SQLiteデータベース管理クラス
    /// </summary>
    public static class Database
    {
        // データベースファイルのパス
        private static readonly string DbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StickyNoteApp",
            "stickynotes.db"
        );

        // 接続文字列
        private static readonly string ConnectionString = $"Data Source={DbPath}";

        /// <summary>
        /// 接続文字列を取得
        /// </summary>
        public static string GetConnectionString() => ConnectionString;

        /// <summary>
        /// データベース初期化
        /// </summary>
        public static void DatabaseInitialize()
        {
            try
            {
                // SQLitePCLの初期化 調査要
                // 
                SQLitePCL.Batteries.Init();

                // ディレクトリが存在しない場合は作成
                string directory = Path.GetDirectoryName(DbPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // テーブル作成
                using (SqliteConnection con = new SqliteConnection(ConnectionString))
                {
                    con.Open();

                    // StickyNotesテーブル存在確認
                    if (!TableExists(con, "StickyNotes"))
                    {
                        System.Diagnostics.Debug.WriteLine("StickyNotesテーブルが存在しません。作成します。");
                        CreateStickyNotesTable(con);
                    }

                }

                System.Diagnostics.Debug.WriteLine("データベース初期化完了");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"データベース初期化エラー: {ex.Message}");
                throw new Exception($"データベース初期化エラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// テーブルの存在確認
        /// </summary>
        public static bool TableExists(SqliteConnection con, string tableName)
        {
            string sql = "SELECT name FROM sqlite_master WHERE type='table' AND name=$tableName";

            using (var cmd = new SqliteCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("$tableName", tableName);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.HasRows;
                }
            }
        }

        /// <summary>
        /// StickyNotesテーブルの作成
        /// </summary>
        private static void CreateStickyNotesTable(SqliteConnection con)
        {
            string sql = @"
                CREATE TABLE StickyNotes (
                    Id TEXT PRIMARY KEY,
                    Content TEXT,
                    PosX INTEGER,
                    PosY INTEGER,
                    Width INTEGER,
                    Height INTEGER,
                    BgR INTEGER,
                    BgG INTEGER,
                    BgB INTEGER,
                    TopMostFlag INTEGER,
                    DeleteFlag INTEGER DEFAULT 0,
                    CreatedAt TEXT,
                    UpdatedAt TEXT
                );
            ";
            using (var cmd = new SqliteCommand(sql, con))
            {
                cmd.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("StickyNotesテーブル作成完了");
            }
        }


        /// <summary>
        /// 付箋データを保存または更新
        /// </summary>
        public static void SaveOrUpdate(StickyNoteForm note)
        {
            try
            {
                using (SqliteConnection con = new SqliteConnection(ConnectionString))
                {
                    con.Open();

                    // UPSERT クエリ
                    string sql = @"
                        INSERT INTO StickyNotes
                        (Id, Content, PosX, PosY, Width, Height, BgR, BgG, BgB, TopMostFlag, DeleteFlag, CreatedAt, UpdatedAt)
                        VALUES
                        ($Id, $Content, $PosX, $PosY, $Width, $Height, $BgR, $BgG, $BgB, $TopMostFlag, 0, $CreatedAt, $UpdatedAt)
                        ON CONFLICT(Id) DO UPDATE SET
                            Content = excluded.Content,
                            PosX = excluded.PosX,
                            PosY = excluded.PosY,
                            Width = excluded.Width,
                            Height = excluded.Height,
                            BgR = excluded.BgR,
                            BgG = excluded.BgG,
                            BgB = excluded.BgB,
                            TopMostFlag = excluded.TopMostFlag,
                            UpdatedAt = excluded.UpdatedAt;
                    ";

                    // 付箋データを保存または更新
                    using (var cmd = new SqliteCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("$Id", note.NoteId);
                        cmd.Parameters.AddWithValue("$Content", note.txtNote.Text ?? "");
                        cmd.Parameters.AddWithValue("$PosX", note.Left);
                        cmd.Parameters.AddWithValue("$PosY", note.Top);
                        cmd.Parameters.AddWithValue("$Width", note.Width);
                        cmd.Parameters.AddWithValue("$Height", note.Height);
                        cmd.Parameters.AddWithValue("$BgR", note.BackColor.R);
                        cmd.Parameters.AddWithValue("$BgG", note.BackColor.G);
                        cmd.Parameters.AddWithValue("$BgB", note.BackColor.B);
                        cmd.Parameters.AddWithValue("$TopMostFlag", note.TopMost ? 1 : 0);
                        cmd.Parameters.AddWithValue("$CreatedAt", note.CreatedAt);
                        cmd.Parameters.AddWithValue("$UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        // 
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"データ保存エラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 付箋データを論理削除（DeleteFlag を 1 に設定）
        /// </summary>
        public static void SoftDelete(string id)
        {
            try
            {
                using (SqliteConnection con = new SqliteConnection(ConnectionString))
                {
                    con.Open();
                    string sql = @"UPDATE StickyNotes SET DeleteFlag = 1, UpdatedAt = $UpdatedAt WHERE Id = $Id";

                    using (var cmd = new SqliteCommand(sql, con))
                    {
                        // 
                        cmd.Parameters.AddWithValue("$Id", id);
                        cmd.Parameters.AddWithValue("$UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"データ削除エラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 全付箋データを取得（DeleteFlag = 0 のもののみ）
        /// </summary>
        public static SqliteDataReader LoadAll()
        {
            try
            {
                var con = new SqliteConnection(ConnectionString);
                con.Open();

                var cmd = con.CreateCommand();
                cmd.CommandText = "SELECT * FROM StickyNotes WHERE DeleteFlag = 0 ORDER BY CreatedAt ASC";

                return cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                throw new Exception($"データ読み込みエラー: {ex.Message}", ex);
            }
        }
    }
}