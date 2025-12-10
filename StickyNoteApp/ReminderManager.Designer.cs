using System;
using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// リマインダー管理クラス（UI部分）
    /// </summary>
    partial class ReminderManager
    {
        /// <summary>
        /// カスタム時間指定ダイアログのフォーム
        /// </summary>
        private Form customReminderForm;
        private Label timeLabel;
        private NumericUpDown minutesUpDown;
        private Label minutesLabel;
        private Button okButton;
        private Button cancelButton;

        /// <summary>
        /// カスタム時間指定ダイアログのコンポーネントを初期化
        /// </summary>
        private void InitializeCustomReminderDialog()
        {
            this.customReminderForm = new Form();
            this.timeLabel = new Label();
            this.minutesUpDown = new NumericUpDown();
            this.minutesLabel = new Label();
            this.okButton = new Button();
            this.cancelButton = new Button();

            // ------- フォーム -------
            this.customReminderForm.Text = "リマインダー時間設定";
            this.customReminderForm.Width = 300;
            this.customReminderForm.Height = 150;
            this.customReminderForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.customReminderForm.StartPosition = FormStartPosition.CenterParent;
            this.customReminderForm.MaximizeBox = false;
            this.customReminderForm.MinimizeBox = false;

            // ------- ラベル（説明文） -------
            this.timeLabel.Text = "時間(分単位)を入力してください（1〜1440分）:";
            this.timeLabel.Left = 20;
            this.timeLabel.Top = 20;
            this.timeLabel.Width = 250;

            // ------- 数値入力 -------
            this.minutesUpDown.Left = 20;
            this.minutesUpDown.Top = 50;
            this.minutesUpDown.Width = 100;
            this.minutesUpDown.Minimum = 1;
            this.minutesUpDown.Maximum = 1440; // 24時間まで
            this.minutesUpDown.Value = 5;

            // ------- ラベル（「分後」） -------
            this.minutesLabel.Text = "分後";
            this.minutesLabel.Left = 125;
            this.minutesLabel.Top = 53;
            this.minutesLabel.Width = 40;

            // ------- OKボタン -------
            this.okButton.Text = "設定";
            this.okButton.Left = 100;
            this.okButton.Top = 80;
            this.okButton.Width = 80;
            this.okButton.DialogResult = DialogResult.OK;

            // ------- キャンセルボタン -------
            this.cancelButton.Text = "キャンセル";
            this.cancelButton.Left = 190;
            this.cancelButton.Top = 80;
            this.cancelButton.Width = 80;
            this.cancelButton.DialogResult = DialogResult.Cancel;

            // ------- コントロール追加 -------
            this.customReminderForm.Controls.Add(this.timeLabel);
            this.customReminderForm.Controls.Add(this.minutesUpDown);
            this.customReminderForm.Controls.Add(this.minutesLabel);
            this.customReminderForm.Controls.Add(this.okButton);
            this.customReminderForm.Controls.Add(this.cancelButton);

            this.customReminderForm.AcceptButton = this.okButton;
            this.customReminderForm.CancelButton = this.cancelButton;
        }

    }
}