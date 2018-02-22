﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using TMTControls.TMTDialogs;

namespace TMTControls
{
    [ToolboxItem(false)]
    public partial class BaseUserControl : UserControl
    {
        public BaseUserControl()
        {
            InitializeComponent();
        }

        [Category("TMT")]
        public event EventHandler NavigateBack;

        protected T NavigateToPanel<T>() where T : UserControl
        {
            return (this.ParentForm as TMTFormMain)?.LoadPanel(typeof(T)) as T;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                if (keyData == Keys.Escape)
                {
                    this.buttonNavigateBack.PerformClick();
                }
            }
            catch { }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BaseUserControl_Load(object sender, EventArgs e)
        {
            try
            {
                this.toolStripStatusLabelWindowName.Text = this.Name;
                this.SetServerInformation();

                buttonNavigateBack.UpdateIcon();

                var iconProperties = new FontAwesome5.Properties(FontAwesome5.Type.Cog)
                {
                    ForeColor = Color.FromArgb(154, 189, 224),
                    Size = 16,
                };
                toolStripSplitButtonOptions.Image = iconProperties.AsImage();
            }
            catch (Exception ex)
            {
                TMTErrorDialog.Show(this, ex, Properties.Resources.ERROR_PanelLoadIssue);
            }
        }

        private void BaseUserControl_Validated(object sender, EventArgs e)
        {
            this.SetServerInformation();
        }

        private void ButtonNavigateBack_Click(object sender, EventArgs e)
        {
            try
            {
                NavigateBack?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                TMTErrorDialog.Show(this, ex, Properties.Resources.ERROR_NavigationBack);
            }
        }

        private void SetServerInformation()
        {
            string server = Properties.Settings.Default.ServerURL;
            if (string.IsNullOrWhiteSpace(server))
            {
                server = $"{Properties.Settings.Default.DatabaseServerName}:{Properties.Settings.Default.DatabaseServerPort}";
            }
            toolStripStatusLabelFill.Text = $"{Properties.Settings.Default.LogInUserId} at {server}";
        }

        private void ToolStripSplitButtonOptions_ButtonClick(object sender, EventArgs e)
        {
            try
            {
                if (captureScreenToolStripMenuItem.Checked)
                {
                    Rectangle bounds = this.ParentForm.Bounds;
                    using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                    {
                        using (var g = Graphics.FromImage(bitmap))
                        {
                            g.CopyFromScreen(this.ParentForm.Location, Point.Empty, bounds.Size);
                        }
                        Clipboard.SetImage(bitmap);
                        toolStripStatusLabelFill.Text = "Window Screen Caputred to Clipboard";
                    }
                }
            }
            catch
            { }
        }
    }
}