using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace StickyNoteApp
{
    /// <summary>
    /// SQLiteデータベース管理クラス
    /// ※ DB操作ごとに接続(open)し、処理完了後にcloseする設計
    /// </summary>
    public static class Database
    {
        // 「SQLite Error 5: 'database is locked'.」防止
        // DB保存処理の排他制御用ロックオブジェクト
        private static readonly object saveLock = new object();

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
        /// 新しいDB接続を生成する（時点では open されていない）
        /// </summary>
        private static SqliteConnection CreateConnection()
        {
            return new SqliteConnection(ConnectionString);
        }

        /// <summary>
        /// SQLite データベース接続を生成し、Open した状態で返す。
        /// 接続直後にコマンドを実行し、接続が有効であることを確認する。
        /// </summary>
        private static SqliteConnection OpenConnection()
        {
            var con = CreateConnection();
            con.Open();

            using (var cmd = con.CreateCommand())
            {
                cmd.ExecuteNonQuery();
            }

            return con;
        }


        /// <summary>
        /// データベース初期化（アプリ起動時に1回だけ呼ぶ）
        /// </summary>
        public static void InitializeDatabase()
        {
            try
            {
                // SQLitePCLの初期化
                // SQLiteのネイティブライブラリを読み込むために必要
                // この初期化がないとデータベース接続時にエラーが発生する
                // アプリケーション全体で1回だけ実行すれば十分
                SQLitePCL.Batteries.Init();

                // ディレクトリが存在しない場合は作成
                string directory = Path.GetDirectoryName(DbPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // テーブル作成
                using (SqliteConnection con = OpenConnection()) // ← 変更: OpenConnection を使う
                {
                    // StickyNotesテーブル存在確認
                    if (!TableExists(con, "StickyNotes"))
                    {
                        System.Diagnostics.Debug.WriteLine("StickyNotesテーブルが存在しません。作成します。");
                        CreateStickyNotesTable(con);
                    }
                    else
                    {
                        // 既存のテーブルにカラムを追加
                        if (!ColumnExists(con, "StickyNotes", "ImagePath"))
                        {
                            System.Diagnostics.Debug.WriteLine("ImagePathカラムを追加します。");
                            AddColumn(con, "StickyNotes", "ImagePath", "TEXT");
                        }

                        // リマインダー関連カラムを追加
                        if (!ColumnExists(con, "StickyNotes", "ReminderActive"))
                        {
                            System.Diagnostics.Debug.WriteLine("ReminderActiveカラムを追加します。");
                            AddColumn(con, "StickyNotes", "ReminderActive", "INTEGER DEFAULT 0");
                        }

                        if (!ColumnExists(con, "StickyNotes", "ReminderTime"))
                        {
                            System.Diagnostics.Debug.WriteLine("ReminderTimeカラムを追加します。");
                            AddColumn(con, "StickyNotes", "ReminderTime", "TEXT");
                        }
                    }
                } //← usingを抜けると自動的にclose

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
        /// カラムの存在確認
        /// </summary>
        private static bool ColumnExists(SqliteConnection con, string tableName, string columnName)
        {
            string sql = $"PRAGMA table_info({tableName})";

            using (var cmd = new SqliteCommand(sql, con))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader["name"].ToString() == columnName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// カラムを追加（汎用）
        /// </summary>
        private static void AddColumn(SqliteConnection con, string tableName, string columnName, string columnType)
        {
            string sql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}";
            using (var cmd = new SqliteCommand(sql, con))
            {
                cmd.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine($"{columnName}カラム追加完了");
            }
        }

        /// <summary>
        /// StickyNotesテーブルの作成(リマインダー関連追加)
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
                    ImagePath TEXT,
                    ReminderActive INTEGER DEFAULT 0,
                    ReminderTime TEXT,
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
        /// SQLパラメータの設定（INSERT or UPDATE 用）
        /// </summary>
        public static void SaveOrUpdate(StickyNoteForm note)
        {
            // 復元中は保存しない
            if (Common.IsRestoring)
            {
                System.Diagnostics.Debug.WriteLine("[SaveOrUpdate] 復元中のため保存をスキップ");
                return;
            }

            //  順番待ち処理（ロックエラー防止）
            lock (saveLock)
            {
                try
                {
                    using (SqliteConnection con = OpenConnection()) // ← 変更
                    {
                        // UPSERTクエリ(リマインダー関連追加）
                        string sql = @"
                            INSERT INTO StickyNotes
                            (Id, Content, PosX, PosY, Width, Height, BgR, BgG, BgB, TopMostFlag, DeleteFlag, ImagePath, 
                             ReminderActive, ReminderTime, CreatedAt, UpdatedAt)
                            VALUES
                            ($Id, $Content, $PosX, $PosY, $Width, $Height, $BgR, $BgG, $BgB, $TopMostFlag, 0, $ImagePath,
                             $ReminderActive, $ReminderTime, $CreatedAt, $UpdatedAt)
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
                                ImagePath = excluded.ImagePath,
                                ReminderActive = excluded.ReminderActive,
                                ReminderTime = excluded.ReminderTime,
                                UpdatedAt = excluded.UpdatedAt;
                        ";

                        // UPSERT用SQLパラメータ（付箋データ）の設定
                        using (var cmd = new SqliteCommand(sql, con))
                        {
                            cmd.Parameters.AddWithValue("$Id", note.NoteId);
                            cmd.Parameters.AddWithValue("$Content", string.IsNullOrEmpty(note.txtNote.Text) ? "" : note.txtNote.Text);
                            cmd.Parameters.AddWithValue("$PosX", note.Left);
                            cmd.Parameters.AddWithValue("$PosY", note.Top);
                            cmd.Parameters.AddWithValue("$Width", note.Width);
                            cmd.Parameters.AddWithValue("$Height", note.Height);
                            cmd.Parameters.AddWithValue("$BgR", note.BackColor.R);
                            cmd.Parameters.AddWithValue("$BgG", note.BackColor.G);
                            cmd.Parameters.AddWithValue("$BgB", note.BackColor.B);
                            cmd.Parameters.AddWithValue("$TopMostFlag", note.TopMost ? 1 : 0);
                            cmd.Parameters.AddWithValue("$ImagePath", string.IsNullOrEmpty(note.CapturedImagePath) ? "" : note.CapturedImagePath);

                            // リマインダー情報を保存
                            var reminderInfo = note.GetReminderInfo();
                            cmd.Parameters.AddWithValue("$ReminderActive", reminderInfo.IsActive ? 1 : 0);
                            cmd.Parameters.AddWithValue("$ReminderTime", reminderInfo.IsActive ? reminderInfo.ReminderTime.ToString("yyyy-MM-dd HH:mm:ss") : "");

                            cmd.Parameters.AddWithValue("$CreatedAt", note.CreatedAt);
                            cmd.Parameters.AddWithValue("$UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            // INSERTまたはUPDATE を実行 
                            cmd.ExecuteNonQuery();
                        }
                    }//← usingを抜けるとclose
                }
                catch (Exception ex)
                {
                    throw new Exception($"データ保存エラー: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 付箋データを論理削除（DeleteFlag を 1 に設定）
        /// </summary>
        public static void SoftDelete(string id)
        {
            // 復元中は削除しない
            if (Common.IsRestoring)
            {
                System.Diagnostics.Debug.WriteLine("[SoftDelete] 復元中のため削除をスキップ");
                return;
            }

            // 順番待ち処理（ロックエラー防止）
            lock (saveLock)
            {
                try
                {
                    using (SqliteConnection con = OpenConnection()) // ← 変更
                    {
                        string sql = @"UPDATE StickyNotes SET DeleteFlag = 1, UpdatedAt = $UpdatedAt WHERE Id = $Id";

                        using (var cmd = new SqliteCommand(sql, con))
                        {
                            cmd.Parameters.AddWithValue("$Id", id);
                            cmd.Parameters.AddWithValue("$UpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.ExecuteNonQuery();
                        }
                    }//← using を抜けると close
                }
                catch (Exception ex)
                {
                    throw new Exception($"データ削除エラー: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 全付箋データを取得（DeleteFlag = 0 のもののみ）
        /// 接続をすぐに閉じるため、Listで返す
        /// </summary>
        public static List<StickyNoteData> LoadAll()
        {
            var notes = new List<StickyNoteData>();

            try
            {
                using (SqliteConnection con = OpenConnection())
                {
                    using (var cmd = con.CreateCommand()) // ← 変更
                    {
                        cmd.CommandText = "SELECT * FROM StickyNotes WHERE DeleteFlag = 0 ORDER BY CreatedAt ASC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var noteData = new StickyNoteData
                                {
                                    Id = reader["Id"].ToString(),
                                    Content = reader["Content"].ToString(),
                                    PosX = Convert.ToInt32(reader["PosX"]),
                                    PosY = Convert.ToInt32(reader["PosY"]),
                                    Width = Convert.ToInt32(reader["Width"]),
                                    Height = Convert.ToInt32(reader["Height"]),
                                    BgR = Convert.ToInt32(reader["BgR"]),
                                    BgG = Convert.ToInt32(reader["BgG"]),
                                    BgB = Convert.ToInt32(reader["BgB"]),
                                    TopMostFlag = Convert.ToInt32(reader["TopMostFlag"]),
                                    ImagePath = reader["ImagePath"]?.ToString(),  // ← 追加
                                    ReminderActive = reader["ReminderActive"] != DBNull.Value
                                        ? Convert.ToInt32(reader["ReminderActive"])
                                        : 0,
                                    ReminderTime = reader["ReminderTime"]?.ToString(),
                                    CreatedAt = reader["CreatedAt"].ToString()
                                };

                                notes.Add(noteData);
                            }
                        }
                    } // ← usingを抜けると確実にcloseされる
                }

                System.Diagnostics.Debug.WriteLine($"データベースから{notes.Count}件の付箋を読み込みました");
                return notes;
            }
            catch (Exception ex)
            {
                throw new Exception($"データ読み込みエラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 付箋データ格納用クラス（データベースから読み込んだデータを保持）
        /// </summary>
        public class StickyNoteData
        {
            public string Id { get; set; }
            public string Content { get; set; }
            public int PosX { get; set; }
            public int PosY { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int BgR { get; set; }
            public int BgG { get; set; }
            public int BgB { get; set; }
            public int TopMostFlag { get; set; }
            public string ImagePath { get; set; }

            public int ReminderActive { get; set; }
            public string ReminderTime { get; set; }
            public string CreatedAt { get; set; }
        }
    }
}