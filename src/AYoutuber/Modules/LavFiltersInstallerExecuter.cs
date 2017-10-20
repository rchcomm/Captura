using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AYoutuber.Modules
{
    [RunInstaller(true)]
    public partial class LavFiltersInstallerExecuter : System.Configuration.Install.Installer
    {
        public LavFiltersInstallerExecuter()
        {
            InitializeComponent();
        }

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);

            this.LavFilterInstall();
        }

        private int limitCount = 10 * 60 * 3;   // 3분동안 처리가 되는지 대기한다.
        private int limitIndex = 0;

        private void LavFilterInstall()
        {
            if (!File.Exists(filterInstallerPath))
            {
                string assemblyPath = this.Context.Parameters["assemblypath"];

                var filterDirectory = Path.Combine(Path.GetDirectoryName(assemblyPath), "LavFilters");
                var filterInstallerPath = Path.Combine(filterDirectory, "LAVFilters-Installer.exe");
                ProcessStartInfo info = new ProcessStartInfo();
                info.Arguments = " /VERYSILENT /SUPPERSSMSGBOXES";    // SILENT, VERYSILENT
                info.FileName = filterInstallerPath;
                info.WorkingDirectory = filterDirectory;

                Process process = Process.Start(info);
                while (!process.HasExited && process.Responding)
                {
                    Thread.Sleep(100);
                    if (limitCount < limitIndex++)
                    {
                        process.Kill();
                        throw new InvalidOperationException("LavFilter installation exception!");
                    }
                }
            }
        }

        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);

            this.SetPermission();
        }

        private void SetPermission()
        {
            // 설치된 폴더의 Works 권한 조정
            //string path = Path.Combine(Path.GetDirectoryName(this.Context.Parameters["assemblypath"]), "Works");
            string path = Path.GetDirectoryName(this.Context.Parameters["assemblypath"]);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            DirectorySecurity sec = Directory.GetAccessControl(path);
            // Using this instead of the "Everyone" string means we work on non-English systems.
            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            sec.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            SecurityIdentifier builtinUsers = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            sec.AddAccessRule(new FileSystemAccessRule(builtinUsers, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            Directory.SetAccessControl(path, sec);
        }

        public override void Rollback(IDictionary savedState)
        {
            this.LavFilterUninstall();

            base.Rollback(savedState);
        }

        public override void Uninstall(IDictionary savedState)
        {
            this.LavFilterUninstall();

            base.Uninstall(savedState);
        }

        private string filterDirectory = @"C:\Program Files (x86)\LAV Filters";
        private string filterInstallerPath = @"C:\Program Files (x86)\LAV Filters\unins000.exe";

        private void LavFilterUninstall()
        {
            if (File.Exists(filterInstallerPath))
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.Arguments = " /VERYSILENT /SUPPERSSMSGBOXES";    // SILENT, VERYSILENT
                info.FileName = filterInstallerPath;
                info.WorkingDirectory = filterDirectory;

                Process process = Process.Start(info);
                while (!process.HasExited && process.Responding)
                {
                    Thread.Sleep(100);
                    if (limitCount < limitIndex++)
                    {
                        process.Kill();
                        throw new InvalidOperationException("LavFilter uninstallation exception!");
                    }
                }
            }
        }
    }
}
