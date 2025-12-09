using System;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// リマインダー管理クラス
    /// </summary>
    public class ReminderManager
    {
        private Timer reminderTimer;
        private DateTime reminderTime;
        private string noteContent;
        private string noteId;
        private bool isActive = false;

        public event EventHandler ReminderTriggered; // リマインダー発火イベント

        /// <summary>
        /// リマインダーが設定されているか
        /// </summary>
        public bool IsActive => isActive;

        /// <summary>
        /// リマインダー時刻
        /// </summary>
        public DateTime ReminderTime => reminderTime;

        /// <summary>
        /// リマインダーを設定
        /// </summary>
        public void SetReminder(string noteId, string content, int minutes)
        {
            this.noteId = noteId;
            this.noteContent = content;
            this.reminderTime = DateTime.Now.AddMinutes(minutes);
            this.isActive = true;

            // 既存のタイマーがあれば停止
            if (reminderTimer != null)
            {
                reminderTimer.Stop();
                reminderTimer.Dispose();
            }

            // 新しいタイマーを作成（1秒ごとにチェック）
            reminderTimer = new Timer();
            reminderTimer.Interval = 1000;
            reminderTimer.Tick += ReminderTimer_Tick;
            reminderTimer.Start();

            System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー設定: {minutes}分後 ({reminderTime:HH:mm:ss})");
        }

        /// <summary>
        /// リマインダーをキャンセル
        /// </summary>
        public void CancelReminder()
        {
            if (reminderTimer != null)
            {
                reminderTimer.Stop();
                reminderTimer.Dispose();
                reminderTimer = null;
            }

            isActive = false;
            System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダーキャンセル");
        }

        /// <summary>
        /// タイマーのティック処理
        /// </summary>
        private void ReminderTimer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now >= reminderTime)
            {
                // リマインダー時刻に到達
                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー発火");

                // タイマー停止
                reminderTimer.Stop();
                isActive = false;

                // 通知を表示
                ShowNotification();

                // イベント発火
                ReminderTriggered?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 通知を表示
        /// </summary>
        private void ShowNotification()
        {
            string preview = string.IsNullOrEmpty(noteContent) ? "(内容なし)" :
                (noteContent.Length > 30 ? noteContent.Substring(0, 30) + "..." : noteContent);

            MessageBox.Show(
                $"リマインダー通知\n\n{preview}",
                "付箋リマインダー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public void Dispose()
        {
            if (reminderTimer != null)
            {
                reminderTimer.Stop();
                reminderTimer.Dispose();
                reminderTimer = null;
            }
        }
    }
}