using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RemoteControl.Server
{
    public partial class FrmBase : Form
    {
        private bool enableCancelButton;

        public bool EnableCancelButton
        {
            get { return enableCancelButton; }
            set { enableCancelButton = value; }
        }

        public FrmBase()
        {
            InitializeComponent();
            SetApplicationIcon();
        }

        private void SetApplicationIcon()
        {
            try
            {
                string location = typeof(FrmBase).Assembly.Location;
                if (!string.IsNullOrEmpty(location) && System.IO.File.Exists(location))
                {
                    Icon appIcon = Icon.ExtractAssociatedIcon(location);
                    if (appIcon != null)
                    {
                        this.Icon = appIcon;
                    }
                }
            }
            catch { }
        }

        private void FrmBase_Load(object sender, EventArgs e)
        {
            if (this.enableCancelButton)
            {
                Button btn = new Button();
                btn.Click += (o, args) =>
                    {
                        Quit();
                    };
                this.CancelButton = btn;
            }
        }

        protected void Quit()
        {
            if (this.Modal)
            {
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            }
            else
            {
                this.Close();
            }
        }
    }
}
