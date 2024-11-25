﻿namespace Server_As_A_Service
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ServerProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.ServerInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // ServerProcessInstaller
            // 
            this.ServerProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalService;
            this.ServerProcessInstaller.Password = null;
            this.ServerProcessInstaller.Username = null;
            this.ServerProcessInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.ServerProcessInstaller_AfterInstall);
            // 
            // ServerInstaller
            // 
            this.ServerInstaller.Description = "This service starts the server for the word guessing game";
            this.ServerInstaller.DisplayName = "ServerService";
            this.ServerInstaller.ServiceName = "ServerControl";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ServerProcessInstaller,
            this.ServerInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller ServerProcessInstaller;
        private System.ServiceProcess.ServiceInstaller ServerInstaller;
    }
}