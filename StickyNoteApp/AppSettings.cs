using System;
using System.IO;
using System.Text.Json;

namespace StickyNoteApp
{
    /// <summary>
    /// アプリケーション設定を管理するクラス
    /// 設定値の保存・読み込みを担う
    /// </summary>
    public class AppSettings
    {
        // ------- プロパティ -------

        /// <summary>フォントファミリー名</summary>
        public string FontFamily { get; set; } = AppConstants.SettingsConfig.DEFAULT_FONT_FAMILY;

        /// <summary>フォントサイズ（pt）</summary>
        public float FontSize { get; set; } = AppConstants.SettingsConfig.DEFAULT_FONT_SIZE;

        // ------- 保存先パス -------

        /// <summary>
        /// 設定ファイルの保存先パス
        /// %LOCALAPPDATA%\StickyNoteApp\settings.json
        /// </summary>
        private static string SettingsFilePath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppConstants.SharedConfig.APP_FOLDER_NAME,
                AppConstants.SettingsConfig.SETTINGS_FILE_NAME
            );

        // ------- 保存 -------

        /// <summary>
        /// 設定をJSONファイルに保存する
        /// </summary>
        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsFilePath, json);

                System.Diagnostics.Debug.WriteLine(AppConstants.SettingsMsg.MSG_SAVE_SUCCESS);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    string.Format(AppConstants.SettingsMsg.MSG_SAVE_ERROR, ex.Message));
            }
        }

        // ------- 読み込み -------

        /// <summary>
        /// 設定ファイルからAppSettingsを読み込んで返す
        /// ファイルが存在しない・読み込み失敗時はデフォルト値を返す
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    System.Diagnostics.Debug.WriteLine(AppConstants.SettingsMsg.MSG_FILE_NOT_FOUND);
                    return new AppSettings();
                }

                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings == null)
                    return new AppSettings();

                // 読み込んだ値をバリデーション（範囲外はデフォルトに戻す）
                settings.FontSize = Clamp(
                    settings.FontSize,
                    AppConstants.SettingsConfig.FONT_SIZE_MIN,
                    AppConstants.SettingsConfig.FONT_SIZE_MAX
                );

                if (string.IsNullOrWhiteSpace(settings.FontFamily))
                    settings.FontFamily = AppConstants.SettingsConfig.DEFAULT_FONT_FAMILY;

                System.Diagnostics.Debug.WriteLine(AppConstants.SettingsMsg.MSG_LOAD_SUCCESS);
                return settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    string.Format(AppConstants.SettingsMsg.MSG_LOAD_ERROR, ex.Message));
                return new AppSettings();
            }
        }

        // ------- ユーティリティ -------

        /// <summary>
        /// float値をmin〜maxの範囲に収める
        /// </summary>
        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
