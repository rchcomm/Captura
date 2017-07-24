﻿using System.Configuration;
using System.IO;
using System.Runtime.CompilerServices;

namespace Captura
{
    public partial class Settings : ApplicationSettingsBase
    {
        public static Settings Instance { get; } = (Settings)Synchronized(new Settings());
        
        Settings()
        {
            // Upgrade settings from Previous version
            if (UpdateRequired)
            {
                Upgrade();
                UpdateRequired = false;
            }
        }
        
        T Get<T>([CallerMemberName] string PropertyName = null) => (T)this[PropertyName];

        void Set<T>(T Value, [CallerMemberName] string PropertyName = null) => this[PropertyName] = Value;
        
        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool UpdateRequired
        {
            get => Get<bool>();
            set => Set(value);
        }

        public void EnsureOutPath()
        {
            if (!Directory.Exists(OutPath))
                Directory.CreateDirectory(OutPath);

            var str = this.OutPathWithSession();
            if (!Directory.Exists(str))
                Directory.CreateDirectory(str);
        }
    }
}
