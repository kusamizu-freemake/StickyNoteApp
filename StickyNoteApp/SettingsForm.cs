using System;
using System.Drawing;
using System.Windows.Forms;

namespace StickyNoteApp
{
    /// <summary>
    /// 設定フォーム（フォント・フォントサイズ）
    /// TrayManagerForm の OnSettingClicked から ShowDialog() で呼び出す
    /// </summary>
    public partial class SettingsForm : Form
    {
        // ------- フィールド -------

        // 編集中の設定値（OK を押すまで元の設定には反映しない）
        private AppSettings _editingSettings;

        // ------- コンストラクタ -------

        /// <summary>
        /// 現在の設定を受け取って初期化する
        /// </summary>
        public SettingsForm(AppSettings currentSettings)
        {
            InitializeComponent();

            // 渡された設定のコピーを編集用に保持（キャンセル時に元の値を保てるように）
            _editingSettings = new AppSettings
            {
                FontFamily = currentSettings.FontFamily,
                FontSize   = currentSettings.FontSize
            };

            LoadSettingsToUI();
        }

        // ------- 初期化 -------

        /// <summary>
        /// 現在の設定値を UI コントロールに反映する
        /// </summary>
        private void LoadSettingsToUI()
        {
            txtFontFamily.Text = _editingSettings.FontFamily;
            numFontSize.Value  = (decimal)_editingSettings.FontSize;
            UpdatePreview();
        }

        // ------- フォント選択 -------

        /// <summary>
        /// 「フォントを選択」ボタン押下時：FontDialog でフォントを選択する
        /// </summary>
        private void BtnFontSelect_Click(object sender, EventArgs e)
        {
            using (var dialog = new FontDialog())
            {
                // 現在の設定でダイアログを初期化
                dialog.Font        = new Font(_editingSettings.FontFamily, _editingSettings.FontSize);
                dialog.ShowEffects = false; // 取り消し線・下線などの装飾は非表示

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _editingSettings.FontFamily = dialog.Font.Name;
                    _editingSettings.FontSize   = dialog.Font.Size;

                    // UI にも反映
                    txtFontFamily.Text = _editingSettings.FontFamily;
                    numFontSize.Value  = (decimal)_editingSettings.FontSize;

                    UpdatePreview();
                }
            }
        }

        // ------- フォントサイズ変更 -------

        /// <summary>
        /// フォントサイズの NumericUpDown 値が変わったときにプレビューを更新する
        /// </summary>
        private void NumFontSize_ValueChanged(object sender, EventArgs e)
        {
            _editingSettings.FontSize = (float)numFontSize.Value;
            UpdatePreview();
        }

        // ------- プレビュー更新 -------

        /// <summary>
        /// 設定値を使ってプレビューラベルのフォントを更新する
        /// </summary>
        private void UpdatePreview()
        {
            try
            {
                lblPreview.Font = new Font(_editingSettings.FontFamily, _editingSettings.FontSize);
            }
            catch
            {
                // 無効なフォント名が入力された場合はデフォルトにフォールバック
                lblPreview.Font = new Font(
                    AppConstants.SettingsConfig.DEFAULT_FONT_FAMILY,
                    AppConstants.SettingsConfig.DEFAULT_FONT_SIZE
                );
            }
        }

        // ------- OK / キャンセル -------

        /// <summary>
        /// OK ボタン押下時：編集中の設定を確定して閉じる
        /// </summary>
        private void BtnOk_Click(object sender, EventArgs e)
        {
            _editingSettings.FontSize   = (float)numFontSize.Value;
            _editingSettings.FontFamily = txtFontFamily.Text.Trim();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// キャンセルボタン押下時：変更を破棄して閉じる
        /// </summary>
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // ------- 呼び出し元へ設定を返す -------

        /// <summary>
        /// 確定した設定を返す（TrayManagerForm から呼ぶ）
        /// </summary>
        public AppSettings GetSettings()
        {
            return _editingSettings;
        }
    }
}
