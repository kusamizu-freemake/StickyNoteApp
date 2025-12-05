using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "StickyNoteApp_Mutex", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("すでにアプリが起動しています。", "二重起動防止", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }


            // データベース初期化
            try
            {
                System.Diagnostics.Debug.WriteLine("Startup: データベース初期化開始");
                Database.InitializeDatabase();
                System.Diagnostics.Debug.WriteLine("Startup: データベース初期化完了");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データベース初期化エラー:\n{ex.Message}", "起動エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // エラーの場合は終了
            }

            // 整合性チェック実行
            try
            {
                System.Diagnostics.Debug.WriteLine("Startup: 整合性チェック開始");
                DatabaseIntegrityChecker.CheckAndRepair();
                DatabaseIntegrityChecker.GenerateReport();
                System.Diagnostics.Debug.WriteLine("Startup: 整合性チェック完了");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"整合性チェックエラー:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // アプリケーション実行
            System.Diagnostics.Debug.WriteLine("Startup: TrayManagerForm起動");
            Application.Run(new TrayManagerForm());
        }
    }
}