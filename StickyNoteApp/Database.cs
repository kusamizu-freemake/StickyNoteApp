using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace StickyNoteApp
{
    /// <summary>
    /// SQLiteデータベース管理クラス
    /// ※ DB操作ごとに接続(open)し、処理完了後にcloseする設計
    /// </summary>
    public static class Database
    {
        // 定数定義
        private const int DB_LOCK_RETRY_DELAY_MS = 50; // DBロック解放待ち時間（ミリ秒）


        // 「SQLite Error 5: 'database is locked'.」防止
        // ロックエラー対策用フラグ
        private static bool isSaving = false;
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
        /// 新しいDB接続を生成する（都度接続用）
        /// ※この時点ではopenされていない
        /// </summary>
        private static SqliteConnection CreateConnection()
        {
            return new SqliteConnection(ConnectionString);
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
                using (SqliteConnection con = CreateConnection())
                {
                    con.Open();//← ここでDBに接続

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

                        // 元画像パスのカラム
                        if (!ColumnExists(con, "StickyNotes", "OriginalImagePath"))
                        {
                            System.Diagnostics.Debug.WriteLine("OriginalImagePathカラムを追加します。");
                            AddColumn(con, "StickyNotes", "OriginalImagePath", "TEXT");
                        }

                        // リサイズ済み画像パスのカラム
                        if (!ColumnExists(con, "StickyNotes", "ResizedImagePath"))
                        {
                            System.Diagnostics.Debug.WriteLine("ResizedImagePathカラムを追加します。");
                            AddColumn(con, "StickyNotes", "ResizedImagePath", "TEXT");
                        }

                        // 画像表示高さのカラム
                        if (!ColumnExists(con, "StickyNotes", "ImageDisplayHeight"))
                        {
                            System.Diagnostics.Debug.WriteLine("ImageDisplayHeightカラムを追加します。");
                            AddColumn(con, "StickyNotes", "ImageDisplayHeight", "INTEGER DEFAULT 150");
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

                        // 既存データの移行処理
                        MigrateImageData(con);
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
        /// 既存の画像データを新しい構造に移行
        /// </summary>
        private static void MigrateImageData(SqliteConnection con)
        {
            try
            {
                // ImagePathにデータがあり、OriginalImagePathが空のレコードを移行
                string sql = @"
                    UPDATE StickyNotes 
                    SET OriginalImagePath = ImagePath 
                    WHERE ImagePath IS NOT NULL 
                      AND ImagePath != '' 
                      AND (OriginalImagePath IS NULL OR OriginalImagePath = '')";

                using (var cmd = new SqliteCommand(sql, con))
                {
                    int count = cmd.ExecuteNonQuery();
                    if (count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"画像データを移行しました: {count}件");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"画像データ移行エラー: {ex.Message}");
                // エラーが発生してもアプリは続行
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
        /// StickyNotesテーブルの作成(画像関連追加)
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
                    OriginalImagePath TEXT,
                    ResizedImagePath TEXT,
                    ImageDisplayHeight INTEGER DEFAULT 150,
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
            //  順番待ち処理（ロックエラー防止）
            lock (saveLock)
            {
                // 他の保存処理が終わるまで待つ
                while (isSaving)
                {
                    System.Threading.Thread.Sleep(DB_LOCK_RETRY_DELAY_MS); // 50ミリ秒待機
                }

                // 保存開始フラグを立てる
                isSaving = true;

                try
                {
                    using (SqliteConnection con = CreateConnection())
                    {
                        con.Open();//← 保存処理開始時にopen

                        // UPSERTクエリ(画像関連追加）
                        string sql = @"
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

                            // 画像関連パラメータ
                            cmd.Parameters.AddWithValue("$ImagePath", string.IsNullOrEmpty(note.CapturedImagePath) ? "" : note.CapturedImagePath);
                            cmd.Parameters.AddWithValue("$OriginalImagePath", string.IsNullOrEmpty(note.OriginalImagePath) ? "" : note.OriginalImagePath);
                            cmd.Parameters.AddWithValue("$ResizedImagePath", string.IsNullOrEmpty(note.ResizedImagePath) ? "" : note.ResizedImagePath);
                            cmd.Parameters.AddWithValue("$ImageDisplayHeight", note.ImageDisplayHeight);

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
                finally
                {
                    // 保存終了フラグを下ろす（必ず実行される）
                    isSaving = false;
                }
            }
        }

        /// <summary>
        /// 付箋データを論理削除（DeleteFlag を 1 に設定）
        /// </summary>
        public static void SoftDelete(string id)
        {
            // 順番待ち処理（ロックエラー防止）
            lock (saveLock)
            {
                while (isSaving)
                {
                    System.Threading.Thread.Sleep(DB_LOCK_RETRY_DELAY_MS); // 50ミリ秒待機
                }

                isSaving = true;

                try
                {
                    using (SqliteConnection con = CreateConnection())
                    {
                        con.Open();//← 削除処理開始時にopen
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
                finally
                {
                    // 保存終了フラグを下ろす
                    isSaving = false;
                }
            }
        }

        /// <summary>
        /// 全付箋データを取得（DeleteFlag = 0 のもののみ）
        /// </summary>
        public static SqliteDataReader LoadAll()
        {
            SqliteConnection con = null;
            SqliteCommand cmd = null;

            try
            {
                // 接続を作成して開く
                // DataReaderを呼び出し元で使用するため、このメソッド内ではusingを使わずに接続をopenする
                con = CreateConnection();
                con.Open();//← ここでopen

                // コマンドを作成
                cmd = con.CreateCommand();
                cmd.CommandText = "SELECT * FROM StickyNotes WHERE DeleteFlag = 0 ORDER BY CreatedAt ASC";

                // CloseConnectionを指定すると、ReaderがCloseされたタイミングで、接続も自動的にCloseされる
                // ※ ただし、ExecuteReader 中に例外が発生した場合は自動では Close されない点に注意
                return cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                // 例外発生時はまだ Reader が返っておらず自動で Close されない可能性があるため、
                // 接続（con）とコマンド（cmd）をここで明示的に後始末する。
                // Disposeは内部でCloseも呼ぶため、Close() を個別に呼ぶ必要はなく Dispose() だけで十分
                // 
                cmd?.Dispose();
                con?.Dispose();

                throw new Exception($"データ読み込みエラー: {ex.Message}", ex);
            }
        }
    }
}