using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace StickyNoteApp
{
    /// <summary>
    /// SQLiteデータベース管理クラス
    /// ※ DB操作ごとに接続(open)し、処理完了後にcloseする設計
    /// </summary>
    public static class Database
    {
        // ログメッセージ定数
        private const string MSG_TABLE_NOT_FOUND = "StickyNotesテーブルが存在しません。作成します。";
        private const string MSG_ADD_COLUMN_IMAGE_PATH = "ImagePathカラムを追加します。";
        private const string MSG_ADD_COLUMN_ORIGINAL = "OriginalImagePathカラムを追加します。";
        private const string MSG_ADD_COLUMN_RESIZED = "ResizedImagePathカラムを追加します。";
        private const string MSG_ADD_COLUMN_DISP_HEIGHT = "ImageDisplayHeightカラムを追加します。";
        private const string MSG_ADD_COLUMN_REMINDER_ACTIVE = "ReminderActiveカラムを追加します。";
        private const string MSG_ADD_COLUMN_REMINDER_TIME = "ReminderTimeカラムを追加します。";
        private const string MSG_DBINIT_COMPLETE = "データベース初期化完了";
        private const string MSG_DBINIT_ERROR = "データベース初期化エラー: {0}";
        private const string MSG_IMAGE_MIGRATE_COUNT = "画像データを移行しました: {0}件";
        private const string MSG_IMAGE_MIGRATE_ERROR = "画像データ移行エラー: {0}";
        private const string MSG_COLUMN_ADDED = "{0}カラム追加完了";
        private const string MSG_TABLE_CREATED = "StickyNotesテーブル作成完了";
        private const string MSG_SKIP_SAVE_RESTORING = "[SaveOrUpdate] 復元中のため保存をスキップ";
        private const string MSG_SAVE_ERROR = "データ保存エラー: {0}";
        private const string MSG_SKIP_DELETE_RESTORING = "[SoftDelete] 復元中のため削除をスキップ";
        private const string MSG_DELETE_ERROR = "データ削除エラー: {0}";
        private const string MSG_LOAD_COUNT = "データベースから{0}件の付箋を読み込みました";
        private const string MSG_LOAD_ERROR = "データ読み込みエラー: {0}";

        // DB フォルダ・ファイル名定数
        private const string DB_FOLDER_NAME = "StickyNoteApp";
        private const string DD_FILE_NAME = "stickynotes.db";

        // 日時フォーマット
        private const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

        // 「SQLite Error 5: 'database is locked'.」防止
        // DB保存処理の排他制御用ロックオブジェクト
        private static readonly object SaveLock = new object();

        // データベースファイルのパス
        private static readonly string DbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DB_FOLDER_NAME,
            DD_FILE_NAME
        );

        // 接続文字列
        private static readonly string ConnectionString = $"Data Source={DbPath};";

        /// <summary>
        /// 接続文字列を取得
        /// </summary>
        public static string GetConnectionString() => ConnectionString;

        /// <summary>
        /// 新しいDB接続を生成する(この時点では open されていない)
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
            var Con = CreateConnection();
            Con.Open();

            using (var Cmd = Con.CreateCommand())
            {
                Cmd.ExecuteNonQuery();
            }

            return Con;
        }

        /// <summary>
        /// データベース初期化(アプリ起動時に1回だけ呼ぶ)
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
                string Directory = Path.GetDirectoryName(DbPath);
                if (!System.IO.Directory.Exists(Directory))
                {
                    System.IO.Directory.CreateDirectory(Directory);
                }

                // テーブル作成
                using (SqliteConnection Con = OpenConnection())
                {
                    // StickyNotesテーブル存在確認
                    if (!TableExists(Con, "StickyNotes"))
                    {
                        Debug.WriteLine(MSG_TABLE_NOT_FOUND);
                        CreateStickyNotesTable(Con);
                    }
                    else
                    {
                        // 既存のテーブルにカラムを追加
                        if (!ColumnExists(Con, "StickyNotes", "ImagePath"))
                        {
                            Debug.WriteLine(MSG_ADD_COLUMN_IMAGE_PATH);
                            AddColumn(Con, "StickyNotes", "ImagePath", "TEXT");
                        }

                        // 元画像パスのカラム
                        if (!ColumnExists(Con, "StickyNotes", "OriginalImagePath"))
                        {
                            Debug.WriteLine(MSG_ADD_COLUMN_ORIGINAL);
                            AddColumn(Con, "StickyNotes", "OriginalImagePath", "TEXT");
                        }

                        // リサイズ済み画像パスのカラム
                        if (!ColumnExists(Con, "StickyNotes", "ResizedImagePath"))
                        {
                            Debug.WriteLine(MSG_ADD_COLUMN_RESIZED);
                            AddColumn(Con, "StickyNotes", "ResizedImagePath", "TEXT");
                        }

                        // 画像表示高さのカラム
                        if (!ColumnExists(Con, "StickyNotes", "ImageDisplayHeight"))
                        {
                            Debug.WriteLine(MSG_ADD_COLUMN_DISP_HEIGHT);
                            AddColumn(Con, "StickyNotes", "ImageDisplayHeight", "INTEGER DEFAULT 150");
                        }

                        // リマインダー関連カラムを追加
                        if (!ColumnExists(Con, "StickyNotes", "ReminderActive"))
                        {
                            Debug.WriteLine(MSG_ADD_COLUMN_REMINDER_ACTIVE);
                            AddColumn(Con, "StickyNotes", "ReminderActive", "INTEGER DEFAULT 0");
                        }

                        if (!ColumnExists(Con, "StickyNotes", "ReminderTime"))
                        {
                            Debug.WriteLine(MSG_ADD_COLUMN_REMINDER_TIME);
                            AddColumn(Con, "StickyNotes", "ReminderTime", "TEXT");
                        }

                        // 既存データの移行処理
                        MigrateImageData(Con);
                    }
                } //← usingを抜けると自動的にclose

                Debug.WriteLine(MSG_DBINIT_COMPLETE);
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(string.Format(MSG_DBINIT_ERROR, Ex.Message));
                throw new Exception(string.Format(MSG_DBINIT_ERROR, Ex.Message), Ex);
            }
        }

        /// <summary>
        /// 既存の画像データを新しい構造に移行
        /// </summary>
        private static void MigrateImageData(SqliteConnection Con)
        {
            try
            {
                // ImagePathにデータがあり、OriginalImagePathが空のレコードを移行
                string Sql = @"
                    UPDATE StickyNotes 
                    SET OriginalImagePath = ImagePath 
                    WHERE ImagePath IS NOT NULL 
                      AND ImagePath != '' 
                      AND (OriginalImagePath IS NULL OR OriginalImagePath = '')";

                using (var Cmd = new SqliteCommand(Sql, Con))
                {
                    int Count = Cmd.ExecuteNonQuery();
                    if (Count > 0)
                    {
                        Debug.WriteLine(string.Format(MSG_IMAGE_MIGRATE_COUNT, Count));
                    }
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine(string.Format(MSG_IMAGE_MIGRATE_ERROR, Ex.Message));
                // エラーが発生してもアプリは続行
            }
        }

        /// <summary>
        /// テーブルの存在確認
        /// </summary>
        public static bool TableExists(SqliteConnection Con, string TableName)
        {
            string Sql = "SELECT name FROM sqlite_master WHERE type='table' AND name=$tableName";

            using (var Cmd = new SqliteCommand(Sql, Con))
            {
                Cmd.Parameters.AddWithValue("$tableName", TableName);
                using (var Reader = Cmd.ExecuteReader())
                {
                    return Reader.HasRows;
                }
            }
        }

        /// <summary>
        /// カラムの存在確認
        /// </summary>
        private static bool ColumnExists(SqliteConnection Con, string TableName, string ColumnName)
        {
            string Sql = $"PRAGMA table_info({TableName})";

            using (var Cmd = new SqliteCommand(Sql, Con))
            using (var Reader = Cmd.ExecuteReader())
            {
                while (Reader.Read())
                {
                    if (Reader["name"].ToString() == ColumnName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// カラムを追加(汎用)
        /// </summary>
        private static void AddColumn(SqliteConnection Con, string TableName, string ColumnName, string ColumnType)
        {
            string Sql = $"ALTER TABLE {TableName} ADD COLUMN {ColumnName} {ColumnType}";
            using (var Cmd = new SqliteCommand(Sql, Con))
            {
                Cmd.ExecuteNonQuery();
                Debug.WriteLine(string.Format(MSG_COLUMN_ADDED, ColumnName));
            }
        }

        /// <summary>
        /// StickyNotesテーブルの作成(画像関連追加)
        /// </summary>
        private static void CreateStickyNotesTable(SqliteConnection Con)
        {
            string Sql = @"
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
                    OriginalImagePath TEXT,
                    ResizedImagePath TEXT,
                    ImageDisplayHeight INTEGER DEFAULT 150,
                    ReminderActive INTEGER DEFAULT 0,
                    ReminderTime TEXT,
                    CreatedAt TEXT,
                    UpdatedAt TEXT
                );
            ";
            using (var Cmd = new SqliteCommand(Sql, Con))
            {
                Cmd.ExecuteNonQuery();
                Debug.WriteLine(MSG_TABLE_CREATED);
            }
        }

        /// <summary>
        /// 付箋データを保存または更新(INSERT or UPDATE用)
        /// </summary>
        public static void SaveOrUpdate(StickyNoteForm Note)
        {
            // 復元中は保存しない
            if (Common.IsRestoring)
            {
                Debug.WriteLine(MSG_SKIP_SAVE_RESTORING);
                return;
            }

            // 順番待ち処理（ロックエラー防止）
            lock (SaveLock)
            {
                try
                {
                    using (SqliteConnection Con = OpenConnection())
                    {
                        // UPSERTクエリ(画像関連追加）
                        string Sql = @"
                            INSERT INTO StickyNotes
                            (Id, Content, PosX, PosY, Width, Height, BgR, BgG, BgB, TopMostFlag, DeleteFlag, 
                             ImagePath, OriginalImagePath, ResizedImagePath, ImageDisplayHeight,
                             ReminderActive, ReminderTime, CreatedAt, UpdatedAt)
                            VALUES
                            ($Id, $Content, $PosX, $PosY, $Width, $Height, $BgR, $BgG, $BgB, $TopMostFlag, 0, 
                             $ImagePath, $OriginalImagePath, $ResizedImagePath, $ImageDisplayHeight,
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
                                OriginalImagePath = excluded.OriginalImagePath,
                                ResizedImagePath = excluded.ResizedImagePath,
                                ImageDisplayHeight = excluded.ImageDisplayHeight,
                                ReminderActive = excluded.ReminderActive,
                                ReminderTime = excluded.ReminderTime,
                                UpdatedAt = excluded.UpdatedAt;
                        ";

                        // UPSERT用SQLパラメータ（付箋データ）の設定
                        using (var Cmd = new SqliteCommand(Sql, Con))
                        {
                            Cmd.Parameters.AddWithValue("$Id", Note.NoteId);
                            Cmd.Parameters.AddWithValue("$Content", string.IsNullOrEmpty(Note.txtNote.Text) ? "" : Note.txtNote.Text);
                            Cmd.Parameters.AddWithValue("$PosX", Note.Left);
                            Cmd.Parameters.AddWithValue("$PosY", Note.Top);
                            Cmd.Parameters.AddWithValue("$Width", Note.Width);
                            Cmd.Parameters.AddWithValue("$Height", Note.Height);
                            Cmd.Parameters.AddWithValue("$BgR", Note.BackColor.R);
                            Cmd.Parameters.AddWithValue("$BgG", Note.BackColor.G);
                            Cmd.Parameters.AddWithValue("$BgB", Note.BackColor.B);
                            Cmd.Parameters.AddWithValue("$TopMostFlag", Note.TopMost ? 1 : 0);

                            // 画像関連パラメータ
                            Cmd.Parameters.AddWithValue("$ImagePath", string.IsNullOrEmpty(Note.CapturedImagePath) ? "" : Note.CapturedImagePath);
                            Cmd.Parameters.AddWithValue("$OriginalImagePath", string.IsNullOrEmpty(Note.OriginalImagePath) ? "" : Note.OriginalImagePath);
                            Cmd.Parameters.AddWithValue("$ResizedImagePath", string.IsNullOrEmpty(Note.ResizedImagePath) ? "" : Note.ResizedImagePath);
                            Cmd.Parameters.AddWithValue("$ImageDisplayHeight", Note.ImageDisplayHeight);

                            // リマインダー情報を保存
                            var ReminderInfo = Note.GetReminderInfo();
                            Cmd.Parameters.AddWithValue("$ReminderActive", ReminderInfo.IsActive ? 1 : 0);
                            Cmd.Parameters.AddWithValue("$ReminderTime", ReminderInfo.IsActive ? ReminderInfo.ReminderTime.ToString(DATE_TIME_FORMAT) : "");

                            Cmd.Parameters.AddWithValue("$CreatedAt", Note.CreatedAt);
                            Cmd.Parameters.AddWithValue("$UpdatedAt", DateTime.Now.ToString(DATE_TIME_FORMAT));

                            // INSERTまたはUPDATE を実行
                            Cmd.ExecuteNonQuery();
                        }
                    } //← usingを抜けるとclose
                }
                catch (Exception Ex)
                {
                    Debug.WriteLine(string.Format(MSG_SAVE_ERROR, Ex.Message));
                }
            }
        }

        /// <summary>
        /// 付箋データを論理削除(DeleteFlag を 1 に設定)
        /// </summary>
        public static void SoftDelete(string Id)
        {
            // 復元中は削除しない
            if (Common.IsRestoring)
            {
                Debug.WriteLine(MSG_SKIP_DELETE_RESTORING);
                return;
            }

            // 順番待ち処理（ロックエラー防止）
            lock (SaveLock)
            {
                try
                {
                    using (SqliteConnection Con = OpenConnection())
                    {
                        string Sql = @"UPDATE StickyNotes SET DeleteFlag = 1, UpdatedAt = $UpdatedAt WHERE Id = $Id";

                        using (var Cmd = new SqliteCommand(Sql, Con))
                        {
                            Cmd.Parameters.AddWithValue("$Id", Id);
                            Cmd.Parameters.AddWithValue("$UpdatedAt", DateTime.Now.ToString(DATE_TIME_FORMAT));
                            Cmd.ExecuteNonQuery();
                        }
                    } //← using を抜けると close
                }
                catch (Exception Ex)
                {
                    Debug.WriteLine(string.Format(MSG_DELETE_ERROR, Ex.Message));
                }
            }
        }

        /// <summary>
        /// 全付箋データを取得(DeleteFlag = 0 のもののみ)
        /// 接続をすぐに閉じるため、Listで返す
        /// </summary>
        public static List<StickyNoteData> LoadAll()
        {
            var Notes = new List<StickyNoteData>();

            try
            {
                using (SqliteConnection Con = OpenConnection())
                {
                    using (var Cmd = Con.CreateCommand())
                    {
                        Cmd.CommandText = "SELECT * FROM StickyNotes WHERE DeleteFlag = 0 ORDER BY CreatedAt ASC";

                        using (var Reader = Cmd.ExecuteReader())
                        {
                            while (Reader.Read())
                            {
                                var NoteData = new StickyNoteData
                                {
                                    Id = Reader["Id"].ToString(),
                                    Content = Reader["Content"].ToString(),
                                    PosX = Convert.ToInt32(Reader["PosX"]),
                                    PosY = Convert.ToInt32(Reader["PosY"]),
                                    Width = Convert.ToInt32(Reader["Width"]),
                                    Height = Convert.ToInt32(Reader["Height"]),
                                    BgR = Convert.ToInt32(Reader["BgR"]),
                                    BgG = Convert.ToInt32(Reader["BgG"]),
                                    BgB = Convert.ToInt32(Reader["BgB"]),
                                    TopMostFlag = Convert.ToInt32(Reader["TopMostFlag"]),
                                    ImagePath = Reader["ImagePath"]?.ToString(),
                                    OriginalImagePath = Reader["OriginalImagePath"]?.ToString(),
                                    ResizedImagePath = Reader["ResizedImagePath"]?.ToString(),
                                    ImageDisplayHeight = Reader["ImageDisplayHeight"] != DBNull.Value
                                        ? Convert.ToInt32(Reader["ImageDisplayHeight"])
                                        : 0,
                                    ReminderActive = Reader["ReminderActive"] != DBNull.Value
                                        ? Convert.ToInt32(Reader["ReminderActive"])
                                        : 0,
                                    ReminderTime = Reader["ReminderTime"]?.ToString(),
                                    CreatedAt = Reader["CreatedAt"].ToString()
                                };

                                Notes.Add(NoteData);
                            }
                        }
                    }
                } // ← usingを抜けると確実にcloseされる

                Debug.WriteLine(string.Format(MSG_LOAD_COUNT, Notes.Count));
                return Notes;
            }
            catch (Exception Ex)
            {
                throw new Exception(string.Format(MSG_LOAD_ERROR, Ex.Message), Ex);
            }
        }
    } // ← Databaseクラスの終わり

    /// <summary>
    /// 付箋データ格納用クラス
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
        public string OriginalImagePath { get; set; }
        public string ResizedImagePath { get; set; }
        public int ImageDisplayHeight { get; set; }
        public int ReminderActive { get; set; }
        public string ReminderTime { get; set; }
        public string CreatedAt { get; set; }
    }
}