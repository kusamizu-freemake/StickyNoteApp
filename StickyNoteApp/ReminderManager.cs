using System;
using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// リマインダー管理クラス
    /// </summary>
    public partial class ReminderManager : Form
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
            try
            {
                // 入力値の検証
                if (string.IsNullOrEmpty(noteId))
                {
                    throw new ArgumentException("付箋IDが指定されていません。", nameof(noteId));
                }

                if (minutes <= 0)
                {
                    throw new ArgumentException("時間は1分以上を指定してください。", nameof(minutes));
                }

                if (minutes > 1440) // 24時間以内
                {
                    throw new ArgumentException("時間は24時間(1440分)以内を指定してください。", nameof(minutes));
                }

                this.noteId = noteId;
                this.noteContent = content ?? string.Empty;
                this.reminderTime = DateTime.Now.AddMinutes(minutes);
                this.isActive = true;

                // 既存のタイマーがあれば停止
                if (reminderTimer != null)
                {
                    reminderTimer.Stop();
                    reminderTimer.Tick -= ReminderTimer_Tick;  // イベント解除
                    reminderTimer.Dispose();
                }

                // 新しいタイマーを作成(1秒ごとにチェック)
                reminderTimer = new Timer();
                reminderTimer.Interval = 1000;
                reminderTimer.Tick += ReminderTimer_Tick;
                reminderTimer.Start();

                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー設定: {minutes}分後 ({reminderTime:HH:mm:ss})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー設定エラー: {ex.Message}");
                MessageBox.Show($"リマインダーの設定に失敗しました。\n\n{ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        /// リマインダーをキャンセル
        /// </summary>
        public void CancelReminder()
        {
            try
            {
                if (reminderTimer != null)
                {
                    reminderTimer.Stop();
                    reminderTimer.Tick -= ReminderTimer_Tick; // イベント解除
                    reminderTimer.Dispose();
                    reminderTimer = null;
                }

                isActive = false;
                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダーキャンセル");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダーキャンセルエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// タイマーのティック処理
        /// </summary>
        private void ReminderTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (DateTime.Now >= reminderTime)
                {
                    // リマインダー時刻に到達
                    System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー発火");

                    // タイマー停止
                    reminderTimer.Stop();
                    reminderTimer.Tick -= ReminderTimer_Tick; // イベント解除
                    isActive = false;

                    // 通知を表示
                    ShowNotification();

                    // イベント発火
                    ReminderTriggered?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダータイマーエラー: {ex.Message}");
                // タイマーエラーが発生した場合は停止
                try
                {
                    if (reminderTimer != null)
                    {
                        reminderTimer.Stop();
                        reminderTimer.Tick -= ReminderTimer_Tick; // イベント解除
                        isActive = false;
                    }
                }
                catch
                {
                    // タイマー停止にも失敗した場合は何もしない

                }
            }
        }

        /// <summary>
        /// 通知を表示
        /// </summary>
        private void ShowNotification()
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{noteId}] 通知表示エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public new void Dispose()
        {
            try
            {
                if (reminderTimer != null)
                {
                    reminderTimer.Stop();
                    reminderTimer.Tick -= ReminderTimer_Tick; // イベント解除
                    reminderTimer.Dispose();
                    reminderTimer = null;
                }
                isActive = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{noteId}] Disposeエラー: {ex.Message}");
            }
        }

        // ============================================
        // StickyNoteForm.csから移動
        // ============================================

        /// <summary>
        /// カスタム時間設定ダイアログを表示してリマインダーを設定
        /// </summary>
        public void ShowCustomReminderDialog(string noteId, string noteContent)
        {
            try
            {
                using (var inputForm = CreateCustomReminderForm())
                {
                    if (inputForm.ShowDialog() == DialogResult.OK)
                    {
                        var hours = (int)inputForm.Controls["numericUpDownHours"].Tag;
                        var minutes = (int)inputForm.Controls["numericUpDownMinutes"].Tag;
                        int totalMinutes = (hours * 60) + minutes;

                        // 0時間0分のチェック
                        if (totalMinutes <= 0)
                        {
                            MessageBox.Show("時間は1分以上を指定してください。", "入力エラー",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        SetReminderWithConfirmation(noteId, noteContent, totalMinutes);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{noteId}] カスタムリマインダーダイアログエラー: {ex.Message}");
                MessageBox.Show($"ダイアログの表示に失敗しました。\n\n{ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// OKボタンクリック時の処理
        /// </summary>
        private void OkButton_Click(object sender, EventArgs e,
            System.Windows.Forms.NumericUpDown numericUpDownHours,
            System.Windows.Forms.NumericUpDown numericUpDownMinutes)
        {
            numericUpDownHours.Tag = (int)numericUpDownHours.Value;
            numericUpDownMinutes.Tag = (int)numericUpDownMinutes.Value;
        }

        /// <summary>
        /// リマインダーを設定し、確認メッセージを表示
        /// </summary>
        public void SetReminderWithConfirmation(string noteId, string noteContent, int minutes)
        {
            try
            {
                SetReminder(noteId, noteContent, minutes);

                DateTime reminderTime = DateTime.Now.AddMinutes(minutes);

                // 時間と分を計算して表示
                int hours = minutes / 60;
                int mins = minutes % 60;

                string timeText;
                if (hours > 0 && mins > 0)
                {
                    timeText = $"{hours}時間{mins}分後";
                }
                else if (hours > 0)
                {
                    timeText = $"{hours}時間後";
                }
                else
                {
                    timeText = $"{mins}分後";
                }

                MessageBox.Show(
                    $"{timeText}にリマインダーを通知します。\n\n通知時刻: {reminderTime:HH:mm:ss}",
                    "リマインダー設定",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー確認表示エラー: {ex.Message}");
                MessageBox.Show($"リマインダーの設定に失敗しました。\n\n{ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// リマインダーキャンセルダイアログを表示
        /// </summary>
        public void ShowCancelReminderDialog()
        {
            try
            {
                if (IsActive)
                {
                    CancelReminder();
                    MessageBox.Show("リマインダーをキャンセルしました。", "リマインダー",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("設定されているリマインダーはありません。", "リマインダー",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{noteId}] キャンセルダイアログエラー: {ex.Message}");
                MessageBox.Show($"リマインダーのキャンセルに失敗しました。\n\n{ex.Message}", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}