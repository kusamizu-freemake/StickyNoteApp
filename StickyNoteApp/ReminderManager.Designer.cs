using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// リマインダー管理クラス - デザイナー部分
    /// </summary>
    partial class ReminderManager
    {
        // フォーム設定定数
        private const string FORM_TITLE = "リマインダー時間設定";
        private const int FORM_WIDTH = 350;
        private const int FORM_HEIGHT = 250;

        // ラベルテキスト定数
        private const string LABEL_DESCRIPTION = "通知までの時間を入力してください:";
        private const string LABEL_HOURS = "時間:";
        private const string LABEL_HOURS_UNIT = "時間";
        private const string LABEL_MINUTES = "分:";
        private const string LABEL_MINUTES_UNIT = "分";
        private const string LABEL_NOTE = "※ 0時間0分は設定できません";

        // ボタンテキスト定数
        private const string BTN_OK = "設定";
        private const string BTN_CANCEL = "キャンセル";

        // NumericUpDown 設定定数
        private const int HOURS_MIN = 0;
        private const int HOURS_MAX = 23;
        private const int HOURS_DEFAULT = 0;
        private const int MINUTES_MIN = 0;
        private const int MINUTES_MAX = 59;
        private const int MINUTES_DEFAULT = 5;

        // フォント定数
        private const string FONT_NAME = "Meiryo";
        private const float FONT_SIZE_NOTE = 8F;

        // レイアウト定数
        private const int LABEL_DESC_LEFT = 20;
        private const int LABEL_DESC_TOP = 20;
        private const int LABEL_DESC_WIDTH = 300;

        private const int LABEL_H_LEFT = 20;
        private const int LABEL_H_TOP = 55;
        private const int LABEL_H_WIDTH = 50;
        private const int NUMERIC_H_LEFT = 75;
        private const int NUMERIC_H_TOP = 52;
        private const int NUMERIC_H_WIDTH = 80;
        private const int LABEL_H_UNIT_LEFT = 160;
        private const int LABEL_H_UNIT_TOP = 55;
        private const int LABEL_H_UNIT_WIDTH = 40;

        private const int LABEL_M_LEFT = 20;
        private const int LABEL_M_TOP = 90;
        private const int LABEL_M_WIDTH = 50;
        private const int NUMERIC_M_LEFT = 75;
        private const int NUMERIC_M_TOP = 87;
        private const int NUMERIC_M_WIDTH = 80;
        private const int LABEL_M_UNIT_LEFT = 160;
        private const int LABEL_M_UNIT_TOP = 90;
        private const int LABEL_M_UNIT_WIDTH = 40;

        private const int LABEL_NOTE_LEFT = 20;
        private const int LABEL_NOTE_TOP = 120;
        private const int LABEL_NOTE_WIDTH = 300;

        private const int BTN_OK_LEFT = 130;
        private const int BTN_OK_TOP = 160;
        private const int BTN_OK_WIDTH = 80;
        private const int BTN_CANCEL_LEFT = 220;
        private const int BTN_CANCEL_TOP = 160;
        private const int BTN_CANCEL_WIDTH = 80;

        /// <summary>
        /// カスタムリマインダーダイアログフォームを作成
        /// </summary>
        private Form CreateCustomReminderForm()
        {
            var InputForm = new Form();
            var LabelDesc = new Label();
            var LabelHours = new Label();
            var NumericUpDownHours = new NumericUpDown();
            var LabelHoursUnit = new Label();
            var LabelMinutes = new Label();
            var NumericUpDownMinutes = new NumericUpDown();
            var LabelMinutesUnit = new Label();
            var NoteLabel = new Label();
            var OkButton = new Button();
            var CancelButton = new Button();

            // ------- フォーム -------
            InputForm.Text = FORM_TITLE;
            InputForm.Width = FORM_WIDTH;
            InputForm.Height = FORM_HEIGHT;
            InputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            InputForm.StartPosition = FormStartPosition.CenterParent;
            InputForm.MaximizeBox = false;
            InputForm.MinimizeBox = false;

            // ------- ラベル：説明 -------
            LabelDesc.Text = LABEL_DESCRIPTION;
            LabelDesc.Left = LABEL_DESC_LEFT;
            LabelDesc.Top = LABEL_DESC_TOP;
            LabelDesc.Width = LABEL_DESC_WIDTH;

            // ------- 時間入力 -------
            LabelHours.Text = LABEL_HOURS;
            LabelHours.Left = LABEL_H_LEFT;
            LabelHours.Top = LABEL_H_TOP;
            LabelHours.Width = LABEL_H_WIDTH;

            NumericUpDownHours.Name = CONTROL_NUMERIC_HOURS;
            NumericUpDownHours.Left = NUMERIC_H_LEFT;
            NumericUpDownHours.Top = NUMERIC_H_TOP;
            NumericUpDownHours.Width = NUMERIC_H_WIDTH;
            NumericUpDownHours.Minimum = HOURS_MIN;
            NumericUpDownHours.Maximum = HOURS_MAX;
            NumericUpDownHours.Value = HOURS_DEFAULT;

            LabelHoursUnit.Text = LABEL_HOURS_UNIT;
            LabelHoursUnit.Left = LABEL_H_UNIT_LEFT;
            LabelHoursUnit.Top = LABEL_H_UNIT_TOP;
            LabelHoursUnit.Width = LABEL_H_UNIT_WIDTH;

            // ------- 分入力 -------
            LabelMinutes.Text = LABEL_MINUTES;
            LabelMinutes.Left = LABEL_M_LEFT;
            LabelMinutes.Top = LABEL_M_TOP;
            LabelMinutes.Width = LABEL_M_WIDTH;

            NumericUpDownMinutes.Name = CONTROL_NUMERIC_MINUTES;
            NumericUpDownMinutes.Left = NUMERIC_M_LEFT;
            NumericUpDownMinutes.Top = NUMERIC_M_TOP;
            NumericUpDownMinutes.Width = NUMERIC_M_WIDTH;
            NumericUpDownMinutes.Minimum = MINUTES_MIN;
            NumericUpDownMinutes.Maximum = MINUTES_MAX;
            NumericUpDownMinutes.Value = MINUTES_DEFAULT;

            LabelMinutesUnit.Text = LABEL_MINUTES_UNIT;
            LabelMinutesUnit.Left = LABEL_M_UNIT_LEFT;
            LabelMinutesUnit.Top = LABEL_M_UNIT_TOP;
            LabelMinutesUnit.Width = LABEL_M_UNIT_WIDTH;

            // ------- 注釈 -------
            NoteLabel.Text = LABEL_NOTE;
            NoteLabel.Left = LABEL_NOTE_LEFT;
            NoteLabel.Top = LABEL_NOTE_TOP;
            NoteLabel.Width = LABEL_NOTE_WIDTH;
            NoteLabel.ForeColor = Color.Gray;
            NoteLabel.Font = new Font(FONT_NAME, FONT_SIZE_NOTE);

            // ------- ボタン -------
            OkButton.Text = BTN_OK;
            OkButton.Left = BTN_OK_LEFT;
            OkButton.Top = BTN_OK_TOP;
            OkButton.Width = BTN_OK_WIDTH;
            OkButton.DialogResult = DialogResult.OK;

            CancelButton.Text = BTN_CANCEL;
            CancelButton.Left = BTN_CANCEL_LEFT;
            CancelButton.Top = BTN_CANCEL_TOP;
            CancelButton.Width = BTN_CANCEL_WIDTH;
            CancelButton.DialogResult = DialogResult.Cancel;

            // ------- OKボタンのクリックイベント -------
            OkButton.Click += (S, E) => OkButton_Click(S, E, NumericUpDownHours, NumericUpDownMinutes);

            // ------- コントロール追加 -------
            InputForm.Controls.Add(LabelDesc);
            InputForm.Controls.Add(LabelHours);
            InputForm.Controls.Add(NumericUpDownHours);
            InputForm.Controls.Add(LabelHoursUnit);
            InputForm.Controls.Add(LabelMinutes);
            InputForm.Controls.Add(NumericUpDownMinutes);
            InputForm.Controls.Add(LabelMinutesUnit);
            InputForm.Controls.Add(NoteLabel);
            InputForm.Controls.Add(OkButton);
            InputForm.Controls.Add(CancelButton);

            InputForm.AcceptButton = OkButton;
            InputForm.CancelButton = CancelButton;

            return InputForm;
        }
    }
}