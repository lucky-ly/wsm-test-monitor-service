namespace MonitorService
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
            this.MonitorServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.MonitorServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // MonitorServiceProcessInstaller
            // 
            this.MonitorServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.MonitorServiceProcessInstaller.Password = null;
            this.MonitorServiceProcessInstaller.Username = null;
            // 
            // MonitorServiceInstaller
            // 
            this.MonitorServiceInstaller.DisplayName = "MonitorService";
            this.MonitorServiceInstaller.ServiceName = "MonitorService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.MonitorServiceProcessInstaller,
            this.MonitorServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller MonitorServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller MonitorServiceInstaller;
    }
}