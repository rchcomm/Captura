﻿using Captura.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;

namespace Captura
{
    public partial class Settings
    {
        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool MainWindowTopmost
        {
            get => Get<bool>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool CopyOutPathToClipboard
        {
            get => Get<bool>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue(null)]
        public string AccentColor
        {
            get => Get<string>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("200")]
        public int MainWindowLeft
        {
            get => Get<int>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("200")]
        public int MainWindowTop
        {
            get => Get<int>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool Expanded
        {
            get => Get<bool>();
            set => Set(value);
        }

        [UserScopedSetting]
        public List<RecentItemModel> RecentItems
        {
            get => Get<List<RecentItemModel>>();
            set => Set(value);
        }
        
        [UserScopedSetting]
        [DefaultSettingValue("30")]
        public int RecentMax
        {
            get => Get<int>();
            set => Set(value);
        }
        
        [UserScopedSetting]
        public string OutPath
        {
            get => Get<string>();
            set => Set(value);
        }

        public string OutPathWithWork(int currentWorkNumber = 0)
        {
            if(currentWorkNumber == 0)
            {
                currentWorkNumber = Settings.Instance.CurrentWorkNumber;
            }

            var str = Path.Combine(this.OutPath, "Work_" + Environment.UserName + "_" + currentWorkNumber.ToString().PadLeft(4, '0'));
            if (!Directory.Exists(str))
            {
                Directory.CreateDirectory(str);
            }
            return str;
        }

        [UserScopedSetting]
        public string FFMpegFolder
        {
            get => Get<string>();
            set
            {
                Set(value);

                ServiceProvider.RaiseFFMpegPathChanged();
            }
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool IncludeCursor
        {
            get => Get<bool>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool MinimizeOnStart
        {
            get => Get<bool>();
            set => Set(value);
        }
        
        [UserScopedSetting]
        [DefaultSettingValue("80")]
        public int VideoQuality
        {
            get => Get<int>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("20")]
        public int FrameRate
        {
            get => Get<int>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool MouseClicks
        {
            get => Get<bool>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool KeyStrokes
        {
            get => Get<bool>();
            set => Set(value);
        }
        
        [UserScopedSetting]
        public string Language
        {
            get => Get<string>();
            set => Set(value);
        }

        [UserScopedSetting]
        public List<HotkeyModel> Hotkeys
        {
            get => Get<List<HotkeyModel>>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool MinimizeToTray
        {
            get => Get<bool>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("50")]
        public int AudioQuality
        {
            get => Get<int>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool TrayNotify
        {
            get => Get<bool>();
            set => Set(value);
        }

        #region Transforms
        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool DoResize
        {
            get => Get<bool>();
            set => Set(value);
        }
        
        [UserScopedSetting]
        [DefaultSettingValue("640")]
        public int ResizeWidth
        {
            get => Get<int>();
            set => Set(value);
        }
        
        [UserScopedSetting]
        [DefaultSettingValue("400")]
        public int ResizeHeight
        {
            get => Get<int>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool FlipHorizontal
        {
            get => Get<bool>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool FlipVertical
        {
            get => Get<bool>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("RotateNone")]
        public RotateBy RotateBy
        {
            get => Get<RotateBy>();
            set => Set(value);
        }
        #endregion

        #region Gif
        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool GifRepeat
        {
            get => Get<bool>();
            set => Set(value);
        }
        
        [UserScopedSetting]
        [DefaultSettingValue("0")]
        public int GifRepeatCount
        {
            get => Get<int>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool GifVariable
        {
            get => Get<bool>();
            set => Set(value);
        }
        #endregion

        [UserScopedSetting]
        [DefaultSettingValue("#BDBDBD")]
        public string TopBarColor
        {
            get => Get<string>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("3")]
        public int RegionBorderThickness
        {
            get => Get<int>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("5000")]
        public int ScreenShotNotifyTimeout
        {
            get => Get<int>();
            set => Set(value);
        }

        [UserScopedSetting]
        [DefaultSettingValue("Black")]
        public Color VideoBackgroundColor
        {
            get => Get<Color>();
            set => Set(value);
        }
    }
}
