﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Diagnostics;

namespace IAM.Backup
{
    [RunInstaller(true)]
    public partial class IAMBackupInstaller : Installer
    {
        public IAMBackupInstaller()
        {
            InitializeComponent();

            //EventLog.WriteEntry("IAM.Collector", "Iniciando processo de instalação do serviço", EventLogEntryType.Information);

            ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            //# Service Account Information
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            //# Service Information
            serviceInstaller.DisplayName = "IAM - Backup service";
            serviceInstaller.Description = "Backup service";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            // This must be identical to the WindowsService.ServiceBase name
            // set in the constructor of WindowsService.cs
            serviceInstaller.ServiceName = "IAMBackup";
            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);

            //EventLog.WriteEntry("IAM.Collector", "Processo de instalação do serviço concluído", EventLogEntryType.Information);

        }
    }
}
