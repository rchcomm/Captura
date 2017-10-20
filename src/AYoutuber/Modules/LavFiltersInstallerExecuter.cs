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

            string assemblyPath = this.Context.Parameters["assemblypath"];

            var filterDirectory = Path.Combine(Path.GetDirectoryName(assemblyPath), "LavFilters");
            var filterInstallerPath = Path.Combine(filterDirectory, "LAVFilters-Installer.exe");
            ProcessStartInfo info = new ProcessStartInfo();
            info.Arguments = " /VERYSILENT /SUPPERSSMSGBOXES";    // SILENT, VERYSILENT
            info.FileName = filterInstallerPath;
            info.WorkingDirectory = filterDirectory;

            Process process = Process.Start(info);
            int limitCount = 10 * 60 * 3;   // 3분동안 처리가 되는지 대기한다.
            int limitIndex = 0;
            while (!process.HasExited && process.Responding)
            {
                Thread.Sleep(100);
                if(limitCount < limitIndex++)
                {
                    process.Kill();
                    throw new InvalidOperationException("LavFilter installation exception!");
                }
            }
        }

        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);

            // 설치된 폴더 권한 조정
            string path = Path.GetDirectoryName(this.Context.Parameters["assemblypath"]);
            DirectorySecurity sec = Directory.GetAccessControl(path);
            // Using this instead of the "Everyone" string means we work on non-English systems.
            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            sec.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            Directory.SetAccessControl(path, sec);
        }

        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
        }

        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);

            var filterDirectory = @"C:\Program Files (x86)\LAV Filters";
            var filterInstallerPath = @"C:\Program Files (x86)\LAV Filters\unins000.exe";
            ProcessStartInfo info = new ProcessStartInfo();
            info.Arguments = " /VERYSILENT /SUPPERSSMSGBOXES";    // SILENT, VERYSILENT
            info.FileName = filterInstallerPath;
            info.WorkingDirectory = filterDirectory;

            Process process = Process.Start(info);
            int limitCount = 10 * 60 * 3;   // 3분동안 처리가 되는지 대기한다.
            int limitIndex = 0;
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
