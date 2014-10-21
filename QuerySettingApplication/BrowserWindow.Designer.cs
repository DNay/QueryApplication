using System.Windows.Forms;
//using Skybound.Gecko;
using Gecko;

namespace QuerySettingApplication
{
    partial class BrowserWindow
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            browser = new GeckoWebBrowser();
            browser.Parent = this;
            browser.Dock = DockStyle.Fill;
            //browser.
            // 
            // BrowserWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(browser);
            this.Name = "BrowserWindow";
            this.Text = "BrowserWindow";
            this.ResumeLayout(false);

        }

        #endregion

        private GeckoWebBrowser browser;
    }
}