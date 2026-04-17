using System;

namespace StickyNoteApp
{
    /// <summary>
    /// 付箋ウィンドウ
    /// リマインダーUI（右クリックメニューのリマインダー操作）
    /// </summary>
    public partial class StickyNoteForm
    {
        /// <summary>リマインダーメニュー：カスタム時間指定</summary>
        private void ReminderCustomMenuItem_Click(object sender, EventArgs e)
            => reminderManager?.ShowCustomReminderDialog(NoteId, txtNote.Text);

        /// <summary>リマインダーメニュー：10分後</summary>
        private void Reminder10MinMenuItem_Click(object sender, EventArgs e)
            => reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, AppConstants.StickyNoteFormConfig.REMINDER_TIME_10MIN);

        /// <summary>リマインダーメニュー：30分後</summary>
        private void Reminder30MinMenuItem_Click(object sender, EventArgs e)
            => reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, AppConstants.StickyNoteFormConfig.REMINDER_TIME_30MIN);

        /// <summary>リマインダーメニュー：60分後</summary>
        private void Reminder60MinMenuItem_Click(object sender, EventArgs e)
            => reminderManager?.SetReminderWithConfirmation(NoteId, txtNote.Text, AppConstants.StickyNoteFormConfig.REMINDER_TIME_60MIN);

        /// <summary>リマインダーメニュー：キャンセル</summary>
        private void ReminderCancelMenuItem_Click(object sender, EventArgs e)
            => reminderManager?.ShowCancelReminderDialog();
    }
}
