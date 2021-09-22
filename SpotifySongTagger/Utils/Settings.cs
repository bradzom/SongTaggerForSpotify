﻿using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace SpotifySongTagger.Utils
{
    public enum SettingKey
    {
        IsDarkTheme,
        HidePlayer,
    }
    public class Settings
    {
        private const string FILE = "settings.json";
        public static Settings Instance { get; } = new Settings();


        private const bool IS_DARK_THEME_DEFAULT = true;
        private const bool HIDE_PLAYER_DEFAULT = false;

        private Dictionary<string, string> Dict { get; }
        private Settings()
        {
            var initialized = false;
            if (File.Exists(FILE))
            {
                try
                {
                    var dictStr = File.ReadAllText(FILE);
                    Dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(dictStr);
                    initialized = true;
                }
                catch (Exception e)
                {
                    Log.Warning($"Failed to load settings {e.Message}");
                }
            }
            if (!initialized)
            {
                Dict = new Dictionary<string, string>
                {
                    { SettingKey.IsDarkTheme.ToString(), IS_DARK_THEME_DEFAULT.ToString() }
                };
            }
        }

        public bool IsDarkTheme
        {
            get => TryGetValue(SettingKey.IsDarkTheme, IS_DARK_THEME_DEFAULT, bool.Parse);
            set => SetValue(SettingKey.IsDarkTheme, value.ToString());
        }
        public bool HidePlayer
        {
            get => TryGetValue(SettingKey.HidePlayer, HIDE_PLAYER_DEFAULT, bool.Parse);
            set => SetValue(SettingKey.HidePlayer, value.ToString());
        }

        private T TryGetValue<T>(SettingKey key, T defaultValue, Func<string, T> convert)
        {
            // try to get value from dictionary
            if (!Dict.TryGetValue(key.ToString(), out var value))
            {
                SetValue(key, defaultValue.ToString());
                return defaultValue;
            }

            // convert to target type
            try
            {
                var ret = convert(value);
                return ret;
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to convert setting key={key} value={value} {e.Message}");
                return defaultValue;
            }
        }
        private void SetValue(SettingKey key, string value)
        {
            Dict[key.ToString()] = value;
            try
            {
                var json = JsonConvert.SerializeObject(Dict);
                File.WriteAllText(FILE, json);
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to save settings {e.Message}");
            }
        }
    }
}
