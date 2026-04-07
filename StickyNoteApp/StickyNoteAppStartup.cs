using System;
using System.Threading;
using System.Windows.Forms;

namespace StickyNoteApp
{
    internal static class StickyNoteAppStartup
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 二重起動防止
            using (Mutex Mutex = new Mutex(true, AppConstants.StartupConfig.MUTEX_NAME, out bool CreatedNew))
            {
                if (!CreatedNew)
                {
                    MessageBox.Show(
                        AppConstants.StartupMsg.MSG_ALREADY_RUNNING,
                        AppConstants.StartupTitle.TITLE_DUPLICATE_LAUNCH,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }

            // データベース初期化
            try
            {
                System.Diagnostics.Debug.WriteLine(AppConstants.StartupMsg.MSG_DBINIT_START);
                Database.InitializeDatabase();
                System.Diagnostics.Debug.WriteLine(AppConstants.StartupMsg.MSG_DBINIT_COMPLETE);
            }
            catch (Exception Ex)
            {
                MessageBox.Show(
                    string.Format(AppConstants.StartupMsg.MSG_DBINIT_ERROR, Ex.Message),
                    AppConstants.StartupTitle.TITLE_STARTUP_ERROR,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return; // エラーの場合は終了
            }

            // アプリケーション実行
            System.Diagnostics.Debug.WriteLine(AppConstants.StartupMsg.MSG_TRAYMANAGER_START);
            Application.Run(new TrayManagerForm());
        }
    }
}