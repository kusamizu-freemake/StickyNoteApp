using System;
using System.Threading;
using System.Windows.Forms;

namespace StickyNoteApp
{
    internal static class StickyNoteAppStartup
    {
        // 文字列定数
        private const string MUTEX_NAME = "StickyNoteApp_Mutex";
        private const string MSG_ALREADY_RUNNING = "すでにアプリが起動しています。";
        private const string TITLE_DUPLICATE_LAUNCH = "二重起動防止";
        private const string MSG_DBINIT_START = "Startup: データベース初期化開始";
        private const string MSG_DBINIT_COMPLATE = "Startup: データベース初期化完了";
        private const string MSG_DBINIT_ERROR = "データベース初期化エラー:\n{0}";
        private const string TITLE_STARTUP_ERROR = "起動エラー";
        private const string MSG_TRAYMANEGER_START = "Startup: TrayManagerForm起動";

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 二重起動防止
            using (Mutex Mutex = new Mutex(true, MUTEX_NAME, out bool CreatedNew))
            {
                if (!CreatedNew)
                {
                    MessageBox.Show(MSG_ALREADY_RUNNING, TITLE_DUPLICATE_LAUNCH, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }

            // データベース初期化
            try
            {
                System.Diagnostics.Debug.WriteLine(MSG_DBINIT_START);
                Database.InitializeDatabase();
                System.Diagnostics.Debug.WriteLine(MSG_DBINIT_COMPLATE);
            }
            catch (Exception Ex)
            {
                MessageBox.Show(string.Format(MSG_DBINIT_ERROR, Ex.Message), TITLE_STARTUP_ERROR, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // エラーの場合は終了
            }

            // アプリケーション実行
            System.Diagnostics.Debug.WriteLine(MSG_TRAYMANEGER_START);
            Application.Run(new TrayManagerForm());
        }
    }
}