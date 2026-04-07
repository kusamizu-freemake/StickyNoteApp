using System;

namespace StickyNoteApp
{
    /// <summary>
    /// アプリケーション全体で使用する定数を管理するクラス
    /// </summary>
    public static class AppConstants
    {
        // 共通定数（複数クラスで使用）
        public static class SharedConfig
        {
            // 日時フォーマット
            public const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

            // アプリフォルダ名
            public const string APP_FOLDER_NAME = "StickyNoteApp";
        }

        public static class SharedTitle
        {
            // 汎用ダイアログタイトル（複数クラスで使用）
            public const string ERROR = "エラー";
            public const string COMPLETE = "完了";
            public const string CONFIRM = "確認";
            public const string INFO = "情報";
        }

        public static class SharedImage
        {
            // 拡張子
            public const string EXT_GIF = ".gif";
            public const string EXT_PNG = ".png";
            public const string EXT_JPG = ".jpg";
            public const string EXT_JPEG = ".jpeg";
            public const string EXT_BMP = ".bmp";

            // 画像レイアウト共通
            public const int MIN_TEXT_AREA_HEIGHT = 80;
            public const int DEFAULT_TITLE_BAR_HEIGHT = 40;
            public const string CONTROL_TITLE_BAR = "titleBar";
        }

        // Common.cs
        public static class CommonMsg
        {
            public const string MSG_BEGIN_RESTORE = "[Common] 復元処理開始";
            public const string MSG_END_RESTORE = "[Common] 復元処理完了（保存はまだ無効）";
            public const string MSG_ENABLE_SAVE = "[Common] データベース保存を有効化";
            public const string MSG_BEGIN_IMAGE_MIGRATE = "[Common] 画像データ移行開始";
            public const string MSG_END_IMAGE_MIGRATE = "[Common] 画像データ移行完了";
        }

        // Database.cs
        public static class DatabaseMsg
        {
            public const string MSG_TABLE_NOT_FOUND = "StickyNotesテーブルが存在しません。作成します。";
            public const string MSG_ADD_COLUMN_IMAGE_PATH = "ImagePathカラムを追加します。";
            public const string MSG_ADD_COLUMN_ORIGINAL = "OriginalImagePathカラムを追加します。";
            public const string MSG_ADD_COLUMN_RESIZED = "ResizedImagePathカラムを追加します。";
            public const string MSG_ADD_COLUMN_DISP_HEIGHT = "ImageDisplayHeightカラムを追加します。";
            public const string MSG_ADD_COLUMN_REMINDER_ACTIVE = "ReminderActiveカラムを追加します。";
            public const string MSG_ADD_COLUMN_REMINDER_TIME = "ReminderTimeカラムを追加します。";
            public const string MSG_DBINIT_COMPLETE = "データベース初期化完了";
            public const string MSG_DBINIT_ERROR = "データベース初期化エラー: {0}";
            public const string MSG_IMAGE_MIGRATE_COUNT = "画像データを移行しました: {0}件";
            public const string MSG_IMAGE_MIGRATE_ERROR = "画像データ移行エラー: {0}";
            public const string MSG_COLUMN_ADDED = "{0}カラム追加完了";
            public const string MSG_TABLE_CREATED = "StickyNotesテーブル作成完了";
            public const string MSG_SKIP_SAVE_RESTORING = "[SaveOrUpdate] 復元中のため保存をスキップ";
            public const string MSG_SAVE_ERROR = "データ保存エラー: {0}";
            public const string MSG_SKIP_DELETE_RESTORING = "[SoftDelete] 復元中のため削除をスキップ";
            public const string MSG_DELETE_ERROR = "データ削除エラー: {0}";
            public const string MSG_LOAD_COUNT = "データベースから{0}件の付箋を読み込みました";
            public const string MSG_LOAD_ERROR = "データ読み込みエラー: {0}";
        }

        public static class DatabaseConfig
        {
            // DB フォルダ・ファイル名
            public const string DD_FILE_NAME = "stickynotes.db";
        }

        // DbIntegrityChecker.cs
        public static class IntegrityMsg
        {
            public const string MSG_CHECK_START = "=== データベース整合性チェック開始===";
            public const string MSG_CHECK_START_VERBOSE = "=== データベース整合性チェック開始 ===";
            public const string MSG_CHECK_COMPLETE = "=== データベース整合性チェック完了 ===";
            public const string MSG_OPTIMIZE_COMPLETE = "✓ データベース最適化完了";
            public const string MSG_RECORD_STATS = "レコード統計: 総数={0}, 有効={1}, 削除済み={2}";
            public const string MSG_RECORD_STATS_LABEL = "レコード統計:";
            public const string MSG_TOTAL_RECORDS = "  総レコード数: {0}";
            public const string MSG_ACTIVE_RECORDS = "  有効なレコード: {0}";
            public const string MSG_DELETED_RECORDS = "  削除済みレコード: {0}";
            public const string MSG_EMPTY_CONTENT = "空のContentを持つレコード: {0}件";
            public const string MSG_DUPLICATE_FOUND = "警告: 重複ID発見: {0}件";
            public const string MSG_DUPLICATE_FOUND_EMOJI = "⚠️ 警告: 重複ID発見: {0}件";
            public const string MSG_NO_DUPLICATE = "✓ 重複IDなし";
            public const string MSG_PURGED_COUNT = "物理削除されたレコード: {0}件";
            public const string MSG_NO_PURGE = "物理削除の必要なし";
            public const string MSG_SKIP_PURGE_BUSY = "警告: 古いレコードの削除をスキップしました（データベースビジー）";
            public const string MSG_FIXED_COUNT = "修正されたレコード: {0}件";
            public const string MSG_NO_INVALID_DATA = "✓ 不正なデータなし";
            public const string MSG_INTEGRITY_ERROR = "❌ 整合性チェックエラー: {0}";
            public const string MSG_REPORT_START = "=== データベース詳細レポート ===";
            public const string MSG_REPORT_END = "=== レポート終了 ===";
            public const string MSG_REPORT_EXE_START = "DatabaseIntegrityChecker.GenerateReport() 実行開始";
            public const string MSG_REPORT_ERROR = "❌ レポート生成エラー: {0}";
            public const string MSG_REPORT_ERROR_DEBUG = "レポート生成エラー: {0}";
            public const string MSG_NO_ACTIVE_NOTES = "有効な付箋はありません";
            public const string MSG_ACTIVE_NOTE = "{0}. [有効] {1} ({2})";
            public const string MSG_ACTIVE_NOTE_ID = "   ID: {0}...";
            public const string MSG_DELETED_NOTE = "[削除{0}] {1}";
            public const string MSG_TOP_MOST = "最前面";
            public const string MSG_NORMAL = "通常";
            public const string MSG_EMPTY_CONTENT2 = "(空)";
            public const string MSG_DUPLICATE_ID_ENTRY = "    - {0}...";
        }

        public static class IntegrityTitle
        {
            // ダイアログタイトル
            public const string TITLE_INTEGRITY_RESULT = "データベース整合性チェック結果";
            public const string TITLE_INTEGRITY_ERROR = "整合性チェックエラー";
            public const string TITLE_REPORT_DETAIL = "データベース詳細レポート";
            public const string TITLE_REPORT_ERROR = "レポート生成エラー";
        }

        public static class IntegrityConfig
        {
            public const int BUSY_TIMEOUT_MS = 10000;
            public const int PURGE_DAYS = 30;
            public const int MAX_RETRIES = 3;
            public const int RETRY_WAIT_MS = 1000;
            public const int SQLITE_ERROR_BUSY = 5;
            public const int PREVIEW_MAX_LENGTH = 15;
            public const int ID_PREVIEW_LENGTH = 13;
            public const int ID_SHORT_PREVIEW_LENGTH = 8;
            public const string SQL_BUSY_TIMEOUT = "PRAGMA busy_timeout = {0};";
            public const string LOG_TIME_FORMAT = "HH:mm:ss";
        }

        // HotkeyManager.cs
        public static class HotkeyMsg
        {
            public const string MSG_REGISTER_SUCCESS = "ホットキー登録成功";
            public const string MSG_REGISTER_FAIL = "ホットキー登録失敗";
            public const string MSG_REGISTER_ERROR = "ホットキー登録エラー: {0}";
            public const string MSG_UNREGISTER_DONE = "ホットキー解除完了";
            public const string MSG_UNREGISTER_ERROR = "ホットキー解除エラー: {0}";
            public const string MSG_HOTKEY_NEW_NOTE = "ホットキー: 新しい付箋を作成";
            public const string MSG_HOTKEY_TOGGLE = "ホットキー: 付箋の表示/非表示切り替え";
        }

        public static class HotkeyConfig
        {
            // ホットキーID
            public const int HOTKEY_ID_NEW_NOTE = 1;
            public const int HOTKEY_ID_TOGGLE_NOTES = 2;
        }

        // ImageManager.cs
        public static class ImageManagerMsg
        {
            public const string MSG_DELETE_FILE_ERROR = "画像ファイル削除時にエラーが発生: {0}";
            public const string MSG_DELETE_SUCCESS = "画像削除成功: {0}";
            public const string MSG_DELETE_RETRY = "画像削除リトライ {0}/{1}: {2}";
            public const string MSG_DELETE_FINAL_FAIL = "画像削除失敗（最終リトライ）: {0} - {1}";
            public const string MSG_LOAD_ERROR = "画像読み込みエラー: {0}";
            public const string MSG_AUTO_ADJUST = "付箋サイズを自動調整: {0}px (画像: {1}px, テキスト領域: {2}px確保)";
            public const string MSG_FILE_SIZE_OVER = "選択した画像ファイルのサイズが大きすぎます。\nファイルサイズ: {0:F2}MB\n最大サイズ: 2MB\n\n2MB以下の画像ファイルを選択してください。";
            public const string MSG_LOAD_SUCCESS = "画像を読み込みました。";
            public const string MSG_LOAD_FAILED = "画像の読み込みに失敗しました:\n{0}";
        }

        public static class ImageManagerTitle
        {
            public const string TITLE_FILE_SIZE_ERROR = "ファイルサイズエラー";
            public const string TITLE_OPEN_IMAGE = "画像を選択";
        }

        public static class ImageManagerConfig
        {
            public const int DEFAULT_IMAGE_HEIGHT = 150;
            public const int IMAGE_LEFT_MARGIN = 0;
            public const long MAX_FILE_SIZE_BYTES = 2 * 1024 * 1024; // 2MB
            public const int DELETE_RETRY_WAIT_MS = 100;
            public const string IMAGE_FOLDER_NAME = "Images";
            public const string ORIGINAL_FILE_NAME_FORMAT = "{0}_original_{1:yyyyMMddHHmmss}{2}";
            public const string FILTER_OPEN_IMAGE = "画像ファイル|*.png;*.jpg;*.jpeg;*.gif|PNGファイル|*.png|JPEGファイル|*.jpg;*.jpeg|GIFファイル|*.gif|すべてのファイル|*.*";
        }

        // ImageResizeManager.cs
        public static class ImageResizeManagerMsg
        {
            public const string MSG_NO_ORIGINAL_PATH = "元画像パスが設定されていません";
            public const string MSG_RESIZE_SUCCESS = "リサイズ成功: {0}";
            public const string MSG_RESIZE_SKIPPED = "リサイズをスキップしました（アニメーションGIFまたはエラー）";
            public const string MSG_AUTO_ADJUST = "付箋サイズを自動調整: {0}px (画像: {1}px, テキスト領域: {2}px確保)";
            public const string MSG_NO_IMAGE_TO_SAVE = "保存する画像がありません。";
        }

        public static class ImageResizeManagerConfig
        {
            public const int SIZE_SMALL = 100;
            public const int SIZE_MEDIUM = 150;
            public const int SIZE_LARGE = 200;
            public const int SIZE_EXTRA_LARGE = 250;
            public const string MENU_LABEL_SMALL = "小 (100px)";
            public const string MENU_LABEL_MEDIUM = "中 (150px)";
            public const string MENU_LABEL_LARGE = "大 (200px)";
            public const string MENU_LABEL_EXTRA_LARGE = "特大 (250px)";
        }

        // ImageResizer.cs
        public static class ImageResizerMsg
        {
            public const string MSG_SKIP_ANIMATED_GIF = "アニメーションGIFはリサイズをスキップします: {0}";
            public const string MSG_RESIZE_SAVED = "リサイズ画像を保存しました: {0}";
            public const string MSG_RESIZE_ERROR = "画像リサイズエラー: {0}";
            public const string MSG_RESIZE_ERROR_THROW = "画像のリサイズに失敗しました: {0}";
            public const string MSG_DELETE_RESIZED = "リサイズ済み画像を削除しました: {0}";
            public const string MSG_DELETE_RESIZED_FAIL = "リサイズ済み画像の削除に失敗: {0}";
            public const string MSG_SAVE_CANCELLED = "保存がキャンセルされました";
            public const string MSG_SAVE_TO_USER = "ユーザー指定場所に保存: {0}";
            public const string MSG_SAVE_ERROR = "画像保存エラー: {0}";
            public const string MSG_SAVE_ORIGINAL = "元画像を保存: {0}";
            public const string MSG_SAVE_ORIGINAL_ERROR = "元画像保存エラー: {0}";
            public const string MSG_ANIMATED_GIF_CANNOT_RESIZE = "アニメーションGIFはリサイズできません。";
            public const string MSG_SAVE_COMPLETE = "リサイズ済み画像を保存しました。\n\n保存先: {0}";
            public const string MSG_SAVE_FAILED = "画像の保存に失敗しました:\n{0}";
            public const string MSG_ORIGINAL_SAVE_COMPLETE = "元画像を保存しました。\n\n保存先: {0}";
            public const string MSG_ORIGINAL_SAVE_FAILED = "元画像の保存に失敗しました:\n{0}";
        }

        public static class ImageResizerTitle
        {
            public const string TITLE_SAVE_COMPLETE = "保存完了";
            public const string TITLE_SAVE_RESIZED = "リサイズ済み画像を保存";
            public const string TITLE_SAVE_ORIGINAL = "元画像を保存";
        }

        public static class ImageResizerConfig
        {
            public const string FILTER_RESIZE_IMAGE = "PNG画像|*.png|JPEG画像|*.jpg;*.jpeg|すべてのファイル|*.*";
            public const string FILTER_ORIGINAL_IMAGE = "PNG画像|*.png|JPEG画像|*.jpg;*.jpeg|GIF画像|*.gif|BMP画像|*.bmp|すべてのファイル|*.*";
            public const string RESIZED_FILE_NAME_FORMAT = "{0}_resized_{1}px_{2:yyyyMMddHHmmss}{3}";
            public const string RESIZED_SUGGEST_FORMAT = "{0}_{1}px";
            public const string DEFAULT_EXT_PNG = "png";
            public const string ARG_ERR_IMAGE_NULL = "高さは正の値である必要があります。";
            public const string ARG_ERR_FILE_NOT_FOUND = "元画像ファイルが見つかりません。";
            public const int MIN_WIDTH = 1;
        }

        // ReminderManager.cs
        public static class ReminderManagerMsg
        {
            public const string MSG_SET_FAILED = "リマインダーの設定に失敗しました。\n\n{0}";
            public const string MSG_DIALOG_FAILED = "ダイアログの表示に失敗しました。\n\n{0}";
            public const string MSG_CANCEL_FAILED = "リマインダーのキャンセルに失敗しました。\n\n{0}";
            public const string MSG_CANCEL_SUCCESS = "リマインダーをキャンセルしました。";
            public const string MSG_NO_REMINDER = "設定されているリマインダーはありません。";
            public const string MSG_NOTIFICATION = "リマインダー通知\n\n{0}";
            public const string MSG_MIN_MINUTES = "時間は{0}分以上を指定してください。";
            public const string MSG_MAX_HOURS = "時間は{0}時間({1}分)以内を指定してください。";
            public const string MSG_NO_NOTE_ID = "付箋IDが指定されていません。";
            public const string MSG_CONFIRM_SET = "{0}にリマインダーを通知します。\n\n通知時刻: {1:HH:mm:ss}";
        }

        public static class ReminderManagerTitle
        {
            public const string TITLE_INPUT_ERROR = "入力エラー";
            public const string TITLE_REMINDER = "リマインダー";
            public const string TITLE_REMINDER_SET = "リマインダー設定";
            public const string TITLE_NOTIFICATION = "付箋リマインダー";
        }

        public static class ReminderManagerConfig
        {
            public const int TIMER_INTERVAL_MS = 1000;
            public const int MIN_REMINDER_MINUTES = 1;
            public const int MINUTES_PER_HOUR = 60;
            public const int MAX_REMINDER_HOURS = 24;
            public const int MAX_REMINDER_MINUTES = MAX_REMINDER_HOURS * MINUTES_PER_HOUR;
            public const int COLUMN_NOT_FOUND = -1;
            public const int REMINDER_ENABLED = 1;
            public const string TIME_TEXT_HOURS_MINS = "{0}時間{1}分後";
            public const string TIME_TEXT_HOURS_ONLY = "{0}時間後";
            public const string TIME_TEXT_MINS_ONLY = "{0}分後";
            public const string CONTROL_NUMERIC_HOURS = "NumericUpDownHours";
            public const string CONTROL_NUMERIC_MINUTES = "NumericUpDownMinutes";
        }

        // StickyNoteAppStartup.cs
        public static class StartupMsg
        {
            public const string MSG_ALREADY_RUNNING = "すでにアプリが起動しています。";
            public const string MSG_DBINIT_START = "Startup: データベース初期化開始";
            public const string MSG_DBINIT_COMPLETE = "Startup: データベース初期化完了";
            public const string MSG_DBINIT_ERROR = "データベース初期化エラー:\n{0}";
            public const string MSG_TRAYMANAGER_START = "Startup: TrayManagerForm起動";
        }

        public static class StartupTitle
        {
            public const string TITLE_DUPLICATE_LAUNCH = "二重起動防止";
            public const string TITLE_STARTUP_ERROR = "起動エラー";
        }

        public static class StartupConfig
        {
            public const string MUTEX_NAME = "StickyNoteApp_Mutex";
        }

        // StickyNoteForm.cs
        public static class StickyNoteFormMsg
        {
            public const string MSG_CONFIRM_DELETE = "この付箋を削除しますか？";
            public const string MSG_CONFIRM_REMOVE_IMAGE = "画像を削除しますか？";
            public const string MSG_FINAL_SAVE = "[{0}] 最終保存実行";
            public const string MSG_FINAL_SAVE_FAIL = "[{0}] 最終保存失敗: {1}";
        }

        public static class StickyNoteFormConfig
        {
            public const int RESIZE_BORDER_WIDTH = 8;
            public const int MIN_WIDTH = 150;
            public const int MIN_HEIGHT = 100;
            public const int NEW_NOTE_OFFSET_X = 30;
            public const int NEW_NOTE_OFFSET_Y = 30;
            public const int PREVIEW_TEXT_MAX_LENGTH = 20;
            public const int REMINDER_TIME_10MIN = 10;
            public const int REMINDER_TIME_30MIN = 30;
            public const int REMINDER_TIME_60MIN = 60;
        }

        // TrayManagerForm.cs
        public static class TrayManagerMsg
        {
            public const string MSG_RESTORE_START = "復元開始: {0}件の付箋を処理します";
            public const string MSG_RESTORE_COMPLETE = "付箋復元完了: {0}件, リマインダー復元: {1}件";
            public const string MSG_RESTORE_ERROR = "付箋復元エラー: {0}";
            public const string MSG_REMINDER_RESTORED = "リマインダー復元: {0} - {1}";
            public const string MSG_REMINDER_EXPIRED = "リマインダー期限切れ: {0} - {1}";
            public const string MSG_INTEGRITY_CHECK_RESUME = "[整合性チェック完了] 保存を再開しました";
            public const string MSG_INTEGRITY_CHECK_CONFIRM =
                "データベースの整合性チェックと修正を実行します。\n\n" +
                "実行中は付箋の保存が一時停止されます。\n\n" +
                "実行しますか？";
            public const string MSG_INTEGRITY_CHECK_DONE_CONFIRM =
                "整合性チェックが完了しました。\n\n" +
                "詳細レポートを表示しますか？";
            public const string MSG_INTEGRITY_CHECK_ERROR =
                "整合性チェック中にエラーが発生しました:\n\n{0}";
            public const string MSG_BALLOON_STARTUP_TITLE = "付箋アプリ";
            public const string MSG_BALLOON_STARTUP_TEXT =
                "ショートカットキー:\nCtrl+Shift+N: 新しい付箋\nCtrl+Shift+H: 付箋の表示/非表示";
            public const string MSG_BALLOON_CLICK_TEXT = "タスクトレイで動作中";
            public const string MSG_TRAY_TOOLTIP_TEXT =
                "付箋アプリ\nCtrl+Shift+N: 新規付箋\nCtrl+Shift+H: 表示/非表示";
            public const string MSG_MENU_NEW_NOTE = "新しい付箋を作成";
            public const string MSG_MENU_HIDE_ALL_NOTES = "すべての付箋を非表示";
            public const string MSG_MENU_SHOW_ALL_NOTES = "すべての付箋を表示";
            public const string MSG_MENU_DB_INTEGRITY_CHECK = "データベース整合性チェック";
            public const string MSG_MENU_SETTINGS = "設定";
            public const string MSG_MENU_EXIT = "アプリを終了";
            public const string MSG_MENU_SHORTCUT_NEW_NOTE = "Ctrl+Shift+N";
            public const string MSG_MENU_SHORTCUT_TOGGLE = "Ctrl+Shift+H";
            public const string MSG_SETTING_NOT_READY = "設定画面は準備中です。";
        }

        public static class TrayManagerTitle
        {
            public const string TITLE_INTEGRITY_CHECK = "データベース整合性チェック";
            public const string TITLE_SETTINGS = "設定";
        }

        public static class TrayManagerConfig
        {
            public const int CURSOR_OFFSET = 50;
            public const int TOPMOST_FLAG_ENABLED = 1;
            public const int TRAY_BALLOON_TIP_DURATION = 2000;
            public const int TRAY_BALLOON_TIP_DURATION_SHORT = 1000;
        }
    }
}