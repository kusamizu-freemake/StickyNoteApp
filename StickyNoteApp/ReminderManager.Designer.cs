using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// リマインダー管理クラス - デザイナー部分
    /// </summary>
    partial class ReminderManager
    {
        /// <summary>
        /// カスタムリマインダーダイアログフォームを作成
        /// </summary>
        private Form CreateCustomReminderForm()
        {
            var inputForm = new Form();
            var label = new Label();
            var labelHours = new Label();
            var numericUpDownHours = new NumericUpDown();
            var labelHoursUnit = new Label();
            var labelMinutes = new Label();
            var numericUpDownMinutes = new NumericUpDown();
            var labelMinutesUnit = new Label();
            var noteLabel = new Label();
            var okButton = new Button();
            var cancelButton = new Button();

            // ------- フォーム -------
            inputForm.Text = "リマインダー時間設定";
            inputForm.Width = 350;
            inputForm.Height = 250;
            inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            inputForm.StartPosition = FormStartPosition.CenterParent;
            inputForm.MaximizeBox = false;
            inputForm.MinimizeBox = false;

            // ------- ラベル：説明 -------
            label.Text = "通知までの時間を入力してください:";
            label.Left = 20;
            label.Top = 20;
            label.Width = 300;

            // ------- 時間入力 -------
            labelHours.Text = "時間:";
            labelHours.Left = 20;
            labelHours.Top = 55;
            labelHours.Width = 50;

            numericUpDownHours.Name = "numericUpDownHours";
            numericUpDownHours.Left = 75;
            numericUpDownHours.Top = 52;
            numericUpDownHours.Width = 80;
            numericUpDownHours.Minimum = 0;
            numericUpDownHours.Maximum = 23;
            numericUpDownHours.Value = 0;

            labelHoursUnit.Text = "時間";
            labelHoursUnit.Left = 160;
            labelHoursUnit.Top = 55;
            labelHoursUnit.Width = 40;

            // ------- 分入力 -------
            labelMinutes.Text = "分:";
            labelMinutes.Left = 20;
            labelMinutes.Top = 90;
            labelMinutes.Width = 50;

            numericUpDownMinutes.Name = "numericUpDownMinutes";
            numericUpDownMinutes.Left = 75;
            numericUpDownMinutes.Top = 87;
            numericUpDownMinutes.Width = 80;
            numericUpDownMinutes.Minimum = 0;
            numericUpDownMinutes.Maximum = 59;
            numericUpDownMinutes.Value = 5;

            labelMinutesUnit.Text = "分";
            labelMinutesUnit.Left = 160;
            labelMinutesUnit.Top = 90;
            labelMinutesUnit.Width = 40;

            // ------- 注釈 -------
            noteLabel.Text = "※ 0時間0分は設定できません";
            noteLabel.Left = 20;
            noteLabel.Top = 120;
            noteLabel.Width = 300;
            noteLabel.ForeColor = Color.Gray;
            noteLabel.Font = new Font("Meiryo", 8F);

            // ------- ボタン -------
            okButton.Text = "設定";
            okButton.Left = 130;
            okButton.Top = 160;
            okButton.Width = 80;
            okButton.DialogResult = DialogResult.OK;

            cancelButton.Text = "キャンセル";
            cancelButton.Left = 220;
            cancelButton.Top = 160;
            cancelButton.Width = 80;
            cancelButton.DialogResult = DialogResult.Cancel;

            // ------- OKボタンのクリックイベント -------
            okButton.Click += (s, e) => OkButton_Click(s, e, numericUpDownHours, numericUpDownMinutes);

            // ------- コントロール追加 -------
            inputForm.Controls.Add(label);
            inputForm.Controls.Add(labelHours);
            inputForm.Controls.Add(numericUpDownHours);
            inputForm.Controls.Add(labelHoursUnit);
            inputForm.Controls.Add(labelMinutes);
            inputForm.Controls.Add(numericUpDownMinutes);
            inputForm.Controls.Add(labelMinutesUnit);
            inputForm.Controls.Add(noteLabel);
            inputForm.Controls.Add(okButton);
            inputForm.Controls.Add(cancelButton);

            inputForm.AcceptButton = okButton;
            inputForm.CancelButton = cancelButton;

            return inputForm;
        }
    }
}