using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// リマインダー情報クラス
    /// </summary>
    public class ReminderInfo
    {
        public bool IsActive { get; set; }
        public DateTime ReminderTime { get; set; }
    }

    /// <summary>
    /// リマインダー管理クラス
    /// </summary>
    public partial class ReminderManager : Form
    {
        // 定数定義
        // 数値定数
        private const int TIMER_INTERVAL_MS = 1000; // リマインダー時刻を確認する間隔（ミリ秒）
        private const int MIN_REMINDER_MINUTES = 1;    // リマインダーとして設定できる最小時間（分）
        private const int MINUTES_PER_HOUR = 60;   // 1時間あたりの分数（計算用）
        private const int MAX_REMINDER_HOURS = 24;   // リマインダーとして設定できる最大時間（24時間）
        private const int MAX_REMINDER_MINUTES = MAX_REMINDER_HOURS * MINUTES_PER_HOUR; // 設定可能な最大時間（1440分＝24時間ちょうどまでOK）
        private const int COLUMN_NOT_FOUND = -1;   // カラムが見つからない場合の値
        private const int REMINDER_ENABLED = 1;    // リマインダー有効フラグの値

        // ダイアログタイトル定数
        private const string TITLE_ERROR = "エラー";
        private const string TITLE_INPUT_ERROR = "入力エラー";
        private const string TITLE_REMINDER = "リマインダー";
        private const string TITLE_REMINDER_SET = "リマインダー設定";
        private const string TITLE_NOTIFICATION = "付箋リマインダー";

        // メッセージ定数
        private const string MSG_SET_FAILED = "リマインダーの設定に失敗しました。\n\n{0}";
        private const string MSG_DIALOG_FAILED = "ダイアログの表示に失敗しました。\n\n{0}";
        private const string MSG_CANCEL_FAILED = "リマインダーのキャンセルに失敗しました。\n\n{0}";
        private const string MSG_CANCEL_SUCCESS = "リマインダーをキャンセルしました。";
        private const string MSG_NO_REMINDER = "設定されているリマインダーはありません。";
        private const string MSG_NOTIFICATION = "リマインダー通知\n\n{0}";
        private const string MSG_MIN_MINUTES = "時間は{0}分以上を指定してください。";
        private const string MSG_MAX_HOURS = "時間は{0}時間({1}分)以内を指定してください。";
        private const string MSG_NO_NOTE_ID = "付箋IDが指定されていません。";
        private const string MSG_CONFIRM_SET = "{0}にリマインダーを通知します。\n\n通知時刻: {1:HH:mm:ss}";

        // 時間テキストフォーマット定数
        private const string TIME_TEXT_HOURS_MINS = "{0}時間{1}分後";
        private const string TIME_TEXT_HOURS_ONLY = "{0}時間後";
        private const string TIME_TEXT_MINS_ONLY = "{0}分後";

        // コントロール名定数
        private const string CONTROL_NUMERIC_HOURS = "NumericUpDownHours";
        private const string CONTROL_NUMERIC_MINUTES = "NumericUpDownMinutes";

        // Timerの宣言を明示的にSystem.Windows.Forms.Timerに変更
        private System.Windows.Forms.Timer ReminderTimer;
        private DateTime ReminderTimeValue;
        private string NoteContent;
        private string NoteId;
        private bool IsActiveValue = false;

        // インスタンス管理用（デバッグ確認用）
        private static int InstanceCount = 0;

        // 親フォーム（StickyNoteForm）への参照。親フォームを覚えておく
        private StickyNoteForm ParentForm;

        public event EventHandler ReminderTriggered; // リマインダー発火イベント

        /// <summary>
        /// リマインダーが設定されているか
        /// </summary>
        public bool IsActive => IsActiveValue;

        /// <summary>
        /// リマインダー時刻
        /// </summary>
        public DateTime ReminderTime => ReminderTimeValue;

        /// <summary>
        /// コンストラクタ（親フォームを受け取る）
        /// </summary>
        public ReminderManager(StickyNoteForm Parent)
        {
            // 親フォームを保存
            ParentForm = Parent;
            Interlocked.Increment(ref InstanceCount); // インスタンス数をインクリメント
        }

        /// <summary>
        /// リマインダー情報を取得
        /// </summary>
        public ReminderInfo GetReminderInfo()
        {
            return new ReminderInfo
            {
                IsActive = IsActiveValue,
                ReminderTime = ReminderTimeValue
            };
        }

        /// <summary>
        /// リマインダーを復元（データベースからの読み込み時用）
        /// </summary>
        public void RestoreReminder(string NoteIdParam, string Content, DateTime ReminderTimeParam)
        {
            try
            {
                // 過去の時刻は無視
                if (ReminderTimeParam <= DateTime.Now)
                {
                    return;
                }

                NoteId = NoteIdParam;
                NoteContent = Content ?? string.Empty;
                ReminderTimeValue = ReminderTimeParam;
                IsActiveValue = true;

                // 既存のタイマーがあれば停止
                StopAndDisposeTimer();

                // タイマー作成
                ReminderTimer = new System.Windows.Forms.Timer();
                ReminderTimer.Interval = TIMER_INTERVAL_MS;
                ReminderTimer.Tick += ReminderTimer_Tick;
                ReminderTimer.Start();
            }
            catch
            {
                // リマインダー復元失敗時は無視（付箋自体は使用可能）
            }
        }

        /// <summary>
        /// データベースから読み込んだ付箋データをもとに、
        /// 有効かつ未来の時刻に設定されているリマインダーのみを復元する。
        /// リマインダーが復元された場合は true、復元されなかった場合は false を返す。
        /// </summary>
        public bool RestoreReminderFromDatabase(string NoteIdParam, string Content, Microsoft.Data.Sqlite.SqliteDataReader Reader)
        {
            try
            {
                // ReminderActiveカラムの存在確認と読み取り
                int ReminderActiveOrdinal = COLUMN_NOT_FOUND;
                int ReminderTimeOrdinal = COLUMN_NOT_FOUND;

                try
                {
                    ReminderActiveOrdinal = Reader.GetOrdinal("ReminderActive");
                    ReminderTimeOrdinal = Reader.GetOrdinal("ReminderTime");
                }
                catch
                {
                    // カラムが存在しない場合は復元不要
                    return false;
                }

                // リマインダーが有効かチェック
                if (Reader.IsDBNull(ReminderActiveOrdinal))
                {
                    return false;
                }

                // リマインダーが「有効(1)」かどうかを数値でチェック
                int ReminderActive = Convert.ToInt32(Reader.GetValue(ReminderActiveOrdinal));
                if (ReminderActive != REMINDER_ENABLED) // 1 = 有効
                {
                    return false;
                }

                // リマインダー時刻を取得
                if (Reader.IsDBNull(ReminderTimeOrdinal))
                {
                    return false;
                }

                string ReminderTimeStr = Reader.GetString(ReminderTimeOrdinal);
                if (string.IsNullOrEmpty(ReminderTimeStr))
                {
                    return false;
                }

                DateTime ParsedReminderTime = DateTime.Parse(ReminderTimeStr);

                // 未来の時刻のみ復元
                if (ParsedReminderTime <= DateTime.Now)
                {
                    return false;
                }

                // リマインダーを復元
                RestoreReminder(NoteIdParam, Content, ParsedReminderTime);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// リマインダーを設定
        /// </summary>
        public void SetReminder(string NoteIdParam, string Content, int Minutes)
        {
            try
            {
                // 入力値の検証
                // 付箋IDが空の場合はエラー
                if (string.IsNullOrEmpty(NoteIdParam))
                {
                    throw new ArgumentException(MSG_NO_NOTE_ID, nameof(NoteIdParam));
                }

                if (Minutes < MIN_REMINDER_MINUTES) // 1分未満の場合はエラー
                {
                    throw new ArgumentException(string.Format(MSG_MIN_MINUTES, MIN_REMINDER_MINUTES), nameof(Minutes));
                }

                if (Minutes > MAX_REMINDER_MINUTES) // 最大24時間（1440分）を超える場合はエラー
                {
                    throw new ArgumentException(string.Format(MSG_MAX_HOURS, MAX_REMINDER_HOURS, MAX_REMINDER_MINUTES), nameof(Minutes));
                }

                NoteId = NoteIdParam;
                NoteContent = Content ?? string.Empty;
                ReminderTimeValue = DateTime.Now.AddMinutes(Minutes);
                IsActiveValue = true;

                // 既存のタイマーがあれば停止
                StopAndDisposeTimer();

                // 新しいタイマーを作成(1秒ごとにチェック)
                ReminderTimer = new System.Windows.Forms.Timer();
                ReminderTimer.Interval = TIMER_INTERVAL_MS;
                ReminderTimer.Tick += ReminderTimer_Tick;
                ReminderTimer.Start();

                // 状態変更完了時に保存処理を呼ぶ
                SaveReminderState();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(string.Format(MSG_SET_FAILED, Ex.Message), TITLE_ERROR,
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
                StopAndDisposeTimer();
                ReminderTimer = null;
                IsActiveValue = false;

                // 状態変更完了時に保存処理を呼ぶ
                SaveReminderState();
            }
            catch
            {
                // キャンセル失敗は無視（既に停止している可能性）
            }
        }

        /// <summary>
        /// タイマーを停止・解除する共通処理
        /// </summary>
        private void StopAndDisposeTimer()
        {
            if (ReminderTimer != null)
            {
                ReminderTimer.Stop();
                ReminderTimer.Tick -= ReminderTimer_Tick; // イベント解除
                ReminderTimer.Dispose();
            }
        }

        /// <summary>
        /// タイマーのティック処理
        /// </summary>
        private void ReminderTimer_Tick(object Sender, EventArgs E)
        {
            try
            {
                if (DateTime.Now >= ReminderTimeValue)
                {
                    // タイマー停止
                    ReminderTimer.Stop();
                    ReminderTimer.Tick -= ReminderTimer_Tick; // イベント解除
                    IsActiveValue = false;

                    // 状態変更完了時に保存処理を呼ぶ（リマインダー解除状態を保存）
                    SaveReminderState();

                    // 保存完了後、付箋を最前面に表示してから通知ダイアログを出す
                    // （順番：保存 → 付箋を前面に → 通知表示）
                    ReminderTriggered?.Invoke(this, EventArgs.Empty);

                    // 通知を表示
                    ShowNotification();
                }
            }
            catch
            {
                // タイマーエラーが発生した場合は停止
                try
                {
                    if (ReminderTimer != null)
                    {
                        ReminderTimer.Stop();
                        ReminderTimer.Tick -= ReminderTimer_Tick; // イベント解除
                        IsActiveValue = false;
                    }
                }
                catch
                {
                    // タイマー停止にも失敗した場合は何もしない
                }
            }
        }

        /// <summary>
        /// リマインダー状態を保存
        /// 状態変更完了時に呼ばれる保存処理
        /// </summary>
        private void SaveReminderState()
        {
            try
            {
                if (ParentForm == null) return;

                // 親フォームに付箋の保存を依頼
                ParentForm.SaveCurrentNoteState();
            }
            catch
            {
                // 保存失敗は無視（リマインダー機能は継続）
            }
        }

        /// <summary>
        /// 通知を表示
        /// </summary>
        private void ShowNotification()
        {
            try
            {
                MessageBox.Show(
                    string.Format(MSG_NOTIFICATION, NoteContent),
                    TITLE_NOTIFICATION,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch
            {
                // 通知表示失敗は無視
            }
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public new void Dispose()
        {
            try
            {
                StopAndDisposeTimer();
                ReminderTimer = null;
                IsActiveValue = false;

                try { Interlocked.Decrement(ref InstanceCount); } catch { }
            }
            catch
            {
                // Dispose失敗は無視
            }
        }

        /// <summary>
        /// カスタム時間設定ダイアログを表示してリマインダーを設定
        /// </summary>
        public void ShowCustomReminderDialog(string NoteIdParam, string Content)
        {
            try
            {
                using (var InputForm = CreateCustomReminderForm())
                {
                    if (InputForm.ShowDialog() == DialogResult.OK)
                    {
                        var Hours = (int)InputForm.Controls[CONTROL_NUMERIC_HOURS].Tag;
                        var Minutes = (int)InputForm.Controls[CONTROL_NUMERIC_MINUTES].Tag;
                        int TotalMinutes = (Hours * MINUTES_PER_HOUR) + Minutes;

                        // 0時間0分のチェック
                        if (TotalMinutes < MIN_REMINDER_MINUTES)
                        {
                            MessageBox.Show(string.Format(MSG_MIN_MINUTES, MIN_REMINDER_MINUTES), TITLE_INPUT_ERROR,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        SetReminderWithConfirmation(NoteIdParam, Content, TotalMinutes);
                    }
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(string.Format(MSG_DIALOG_FAILED, Ex.Message), TITLE_ERROR,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// OKボタンクリック時の処理
        /// </summary>
        private void OkButton_Click(object Sender, EventArgs E,
            System.Windows.Forms.NumericUpDown NumericUpDownHours,
            System.Windows.Forms.NumericUpDown NumericUpDownMinutes)
        {
            NumericUpDownHours.Tag = (int)NumericUpDownHours.Value;
            NumericUpDownMinutes.Tag = (int)NumericUpDownMinutes.Value;
        }

        /// <summary>
        /// リマインダーを設定し、確認メッセージを表示
        /// </summary>
        public void SetReminderWithConfirmation(string NoteIdParam, string Content, int Minutes)
        {
            try
            {
                SetReminder(NoteIdParam, Content, Minutes);

                DateTime ConfirmTime = DateTime.Now.AddMinutes(Minutes);

                // 時間と分を計算して表示
                int Hours = Minutes / MINUTES_PER_HOUR;
                int Mins = Minutes % MINUTES_PER_HOUR;

                string TimeText;
                if (Hours > 0 && Mins > 0)
                {
                    TimeText = string.Format(TIME_TEXT_HOURS_MINS, Hours, Mins);
                }
                else if (Hours > 0)
                {
                    TimeText = string.Format(TIME_TEXT_HOURS_ONLY, Hours);
                }
                else
                {
                    TimeText = string.Format(TIME_TEXT_MINS_ONLY, Mins);
                }

                MessageBox.Show(
                    string.Format(MSG_CONFIRM_SET, TimeText, ConfirmTime),
                    TITLE_REMINDER_SET,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception Ex)
            {
                MessageBox.Show(string.Format(MSG_SET_FAILED, Ex.Message), TITLE_ERROR,
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
                    MessageBox.Show(MSG_CANCEL_SUCCESS, TITLE_REMINDER,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(MSG_NO_REMINDER, TITLE_REMINDER,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(string.Format(MSG_CANCEL_FAILED, Ex.Message), TITLE_ERROR,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}