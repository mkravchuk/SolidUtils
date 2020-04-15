namespace SolidUtils.Options
{
    partial class UserControl1
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserControl1));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.testToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.uncheckedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.testToolStripMenuItem,
            this.uncheckedToolStripMenuItem});
            this.contextMenuStrip1.Margin = new System.Windows.Forms.Padding(90, 0, 0, 0);
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.contextMenuStrip1.ShowCheckMargin = true;
            this.contextMenuStrip1.Size = new System.Drawing.Size(244, 97);
            // 
            // testToolStripMenuItem
            // 
            this.testToolStripMenuItem.Checked = true;
            this.testToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.testToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("testToolStripMenuItem.Image")));
            this.testToolStripMenuItem.ImageAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.testToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.testToolStripMenuItem.Name = "testToolStripMenuItem";
            this.testToolStripMenuItem.Size = new System.Drawing.Size(243, 30);
            this.testToolStripMenuItem.Text = "checked";
            // 
            // uncheckedToolStripMenuItem
            // 
            this.uncheckedToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("uncheckedToolStripMenuItem.Image")));
            this.uncheckedToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.uncheckedToolStripMenuItem.Name = "uncheckedToolStripMenuItem";
            this.uncheckedToolStripMenuItem.Size = new System.Drawing.Size(243, 30);
            this.uncheckedToolStripMenuItem.Text = "unchecked";
            // 
            // UserControl1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "UserControl1";
            this.Size = new System.Drawing.Size(714, 498);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem testToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem uncheckedToolStripMenuItem;
    }
}
