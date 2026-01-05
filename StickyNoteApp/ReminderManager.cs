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
        private const int TIMER_INTERVAL_MS = 1000; // リマインダー時刻を確認する間隔（ミリ秒）
        private const int MIN_REMINDER_MINUTES = 1; // リマインダーとして設定できる最小時間（分）
        private const int MINUTES_PER_HOUR = 60; // 1時間あたりの分数（計算用）
        private const int MAX_REMINDER_HOURS = 24; // リマインダーとして設定できる最大時間（24時間）
        private const int MAX_REMINDER_MINUTES = MAX_REMINDER_HOURS * MINUTES_PER_HOUR; // 設定可能な最大時間（1440分＝24時間ちょうどまでOK）

        private const int COLUMN_NOT_FOUND = -1; // カラムが見つからない場合の値
        private const int REMINDER_ENABLED = 1; // リマインダー有効フラグの値

        // Timerの宣言を明示的にSystem.Windows.Forms.Timerに変更
        private System.Windows.Forms.Timer reminderTimer;
        private DateTime reminderTime;
        private string noteContent;
        private string noteId;
        private bool isActive = false;

        // インスタンス管理用（デバッグ確認用）
        private static int instanceCount = 0;

        // 親フォーム（StickyNoteForm）への参照。親フォームを覚えておく
        private StickyNoteForm parentForm;

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
        /// コンストラクタ（親フォームを受け取る）
        /// </summary>
        public ReminderManager(StickyNoteForm parent)
        {
            // 親フォームを保存
            this.parentForm = parent;

            System.Diagnostics.Debug.WriteLine($"[ReminderManager] ctor Hash={this.GetHashCode()} Thread={Thread.CurrentThread.ManagedThreadId}");
            Interlocked.Increment(ref instanceCount); // インスタンス数をインクリメント
            System.Diagnostics.Debug.WriteLine($"[ReminderManager] instanceCount={instanceCount}");
        }

        /// <summary>
        /// ファイナライザ（GCで回収された場合のログ）
        /// </summary>
        ~ReminderManager()
        {
            System.Diagnostics.Debug.WriteLine($"[ReminderManager] Finalizer Hash={this.GetHashCode()} Thread={Thread.CurrentThread.ManagedThreadId}");
            try { Interlocked.Decrement(ref instanceCount); } catch { }
            System.Diagnostics.Debug.WriteLine($"[ReminderManager] instanceCount(after finalizer)={instanceCount}");
        }

        /// <summary>
        /// リマインダー情報を取得
        /// </summary>
        public ReminderInfo GetReminderInfo()
        {
            return new ReminderInfo
            {
                IsActive = isActive,
                ReminderTime = reminderTime
            };
        }

        /// <summary>
        /// リマインダーを復元（データベースからの読み込み時用）
        /// </summary>
        public void RestoreReminder(string noteId, string content, DateTime reminderTime)
        {
            try
            {
                // 過去の時刻は無視
                if (reminderTime <= DateTime.Now)
                {
                    System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー時刻が過去のためスキップ: {reminderTime:yyyy-MM-dd HH:mm:ss}");
                    return;
                }

                this.noteId = noteId;
                this.noteContent = content ?? string.Empty;
                this.reminderTime = reminderTime;
                this.isActive = true;

                // 既存のタイマーがあれば停止
                if (reminderTimer != null)
                {
                    reminderTimer.Stop();
                    reminderTimer.Tick -= ReminderTimer_Tick; // イベント解除
                    reminderTimer.Dispose();
                }

                // タイマー作成
                reminderTimer = new System.Windows.Forms.Timer();
                reminderTimer.Interval = TIMER_INTERVAL_MS;
                reminderTimer.Tick += ReminderTimer_Tick;
                reminderTimer.Start();

                TimeSpan timeLeft = reminderTime - DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー復元: {reminderTime:HH:mm:ss} (残り{timeLeft.TotalMinutes:F1}分) Thread={Thread.CurrentThread.ManagedThreadId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー復元エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// データベースから読み込んだ付箋データをもとに、
        /// 有効かつ未来の時刻に設定されているリマインダーのみを復元する。
        /// リマインダーが復元された場合は true、復元されなかった場合は false を返す。
        /// </summary>
        public bool RestoreReminderFromDatabase(string noteId, string content, Microsoft.Data.Sqlite.SqliteDataReader reader)
        {
            try
            {
                // ReminderActiveカラムの存在確認と読み取り
                int reminderActiveOrdinal = COLUMN_NOT_FOUND;
                int reminderTimeOrdinal = COLUMN_NOT_FOUND;

                try
                {
                    reminderActiveOrdinal = reader.GetOrdinal("ReminderActive");
                    reminderTimeOrdinal = reader.GetOrdinal("ReminderTime");
                }
                catch
                {
                    // カラムが存在しない場合は復元不要
                    System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダーカラムが存在しません");
                    return false;
                }

                // リマインダーが有効かチェック
                if (reader.IsDBNull(reminderActiveOrdinal))
                {
                    return false;
                }

                // リマインダーが「有効(1)」かどうかを数値でチェック
                int reminderActive = Convert.ToInt32(reader.GetValue(reminderActiveOrdinal));
                if (reminderActive != REMINDER_ENABLED) // 1 = 有効
                {
                    return false;
                }

                // リマインダー時刻を取得
                if (reader.IsDBNull(reminderActiveOrdinal))
                {
                    return false;
                }

                string reminderTimeStr = reader.GetString(reminderActiveOrdinal);
                if (string.IsNullOrEmpty(reminderTimeStr))
                {
                    return false;
                }

                DateTime reminderTime = DateTime.Parse(reminderTimeStr);

                // 未来の時刻のみ復元
                if (reminderTime <= DateTime.Now)
                {
                    System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー時刻が過去のため無効化: {reminderTime:yyyy-MM-dd HH:mm:ss}");
                    return false;
                }

                // リマインダーを復元
                RestoreReminder(noteId, content, reminderTime);

                TimeSpan timeLeft = reminderTime - DateTime.Now;
                System.Diagnostics.Debug.WriteLine(
                    $"[{noteId}] リマインダー復元成功: {reminderTime:yyyy-MM-dd HH:mm:ss} (残り{timeLeft.TotalMinutes:F1}分)"
                );

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー復元エラー: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// リマインダーを設定
        /// </summary>
        public void SetReminder(string noteId, string content, int minutes)
        {
            try
            {
                // 入力値の検証
                // 付箋IDが空の場合はエラー
                if (string.IsNullOrEmpty(noteId))
                {
                    throw new ArgumentException("付箋IDが指定されていません。", nameof(noteId));
                }

                if (minutes < MIN_REMINDER_MINUTES) // 1分未満の場合はエラー
                {
                    throw new ArgumentException($"時間は{MIN_REMINDER_MINUTES}分以上を指定してください。", nameof(minutes));
                }

                if (minutes > MAX_REMINDER_MINUTES) // 最大24時間（1440分）を超える場合はエラー
                {
                    throw new ArgumentException($"時間は{MAX_REMINDER_HOURS}時間({MAX_REMINDER_MINUTES}分)以内を指定してください。", nameof(minutes));
                }

                this.noteId = noteId;
                this.noteContent = content ?? string.Empty;
                this.reminderTime = DateTime.Now.AddMinutes(minutes);
                this.isActive = true;

                // 既存のタイマーがあれば停止
                if (reminderTimer != null)
                {
                    reminderTimer.Stop();
                    reminderTimer.Tick -= ReminderTimer_Tick; // イベント解除
                    reminderTimer.Dispose();
                }

                // 新しいタイマーを作成(1秒ごとにチェック)
                reminderTimer = new System.Windows.Forms.Timer();
                reminderTimer.Interval = TIMER_INTERVAL_MS;
                reminderTimer.Tick += ReminderTimer_Tick;
                reminderTimer.Start();

                // 状態変更完了時に保存処理を呼ぶ
                SaveReminderState();

                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー設定: {minutes}分後 ({reminderTime:HH:mm:ss}) Thread={Thread.CurrentThread.ManagedThreadId}");
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

                // 状態変更完了時に保存処理を呼ぶ
                SaveReminderState();

                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダーキャンセル Thread={Thread.CurrentThread.ManagedThreadId}");
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
                System.Diagnostics.Debug.WriteLine($"[{noteId}] ReminderTimer_Tick Thread={Thread.CurrentThread.ManagedThreadId}");

                if (DateTime.Now >= reminderTime)
                {
                    // リマインダー時刻に到達
                    System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー発火");

                    // タイマー停止
                    reminderTimer.Stop();
                    reminderTimer.Tick -= ReminderTimer_Tick; // イベント解除
                    isActive = false;

                    // 状態変更完了時に保存処理を呼ぶ（リマインダー解除状態を保存）
                    SaveReminderState();

                    // 保存完了後、付箋を最前面に表示してから通知ダイアログを出す
                    // （順番：保存 → 付箋を前面に → 通知表示）
                    ReminderTriggered?.Invoke(this, EventArgs.Empty);

                    // 通知を表示
                    ShowNotification();
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
        /// リマインダー状態を保存
        /// 状態変更完了時に呼ばれる保存処理
        /// </summary>
        private void SaveReminderState()
        {
            try
            {
                if (parentForm == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[{noteId}] SaveReminderState: 親フォームが未設定");
                    return;
                }

                // 親フォームに付箋の保存を依頼
                // 親フォーム側で現在の付箋の全情報をDatabase.SaveOrUpdate()に渡す
                parentForm.SaveCurrentNoteState();

                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー状態を保存 (IsActive={isActive})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{noteId}] リマインダー状態保存エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 通知を表示
        /// </summary>
        private void ShowNotification()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[{noteId}] ShowNotification Thread={Thread.CurrentThread.ManagedThreadId}");

                MessageBox.Show(
                    $"リマインダー通知\n\n{noteContent}",
                    "付箋リマインダー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                System.Diagnostics.Debug.WriteLine($"[{noteId}] ShowNotification完了");
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
                System.Diagnostics.Debug.WriteLine($"[{noteId}] Dispose called Hash={this.GetHashCode()} Thread={Thread.CurrentThread.ManagedThreadId}");
                if (reminderTimer != null)
                {
                    reminderTimer.Stop();
                    reminderTimer.Tick -= ReminderTimer_Tick; // イベント解除
                    reminderTimer.Dispose();
                    reminderTimer = null;
                }
                isActive = false;

                try { Interlocked.Decrement(ref instanceCount); } catch { }
                System.Diagnostics.Debug.WriteLine($"[ReminderManager] instanceCount(after Dispose)={instanceCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{noteId}] Disposeエラー: {ex.Message}");
            }
        }

        // --- 以下は既存のダイアログ / ヘルパーコード （省略しない）---

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
                        var hours = (int)inputForm.Controls["NumericUpDownHours"].Tag;
                        var minutes = (int)inputForm.Controls["NumericUpDownMinutes"].Tag;
                        int totalMinutes = (hours * MINUTES_PER_HOUR) + minutes;

                        // 0時間0分のチェック
                        if (totalMinutes < MIN_REMINDER_MINUTES)
                        {
                            MessageBox.Show($"時間は{MIN_REMINDER_MINUTES}分以上を指定してください。", "入力エラー",
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
                int hours = minutes / MINUTES_PER_HOUR;
                int mins = minutes % MINUTES_PER_HOUR;

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