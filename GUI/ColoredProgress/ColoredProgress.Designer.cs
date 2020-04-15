using SolidUtils.GUI.StatusListProgress;

namespace SolidUtils.GUI
{
    partial class ColoredProgress
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ColoredProgress));
            this.buttonCancel = new System.Windows.Forms.Button();
            this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
            this.labelTransparent1 = new SolidUtils.GUI.LabelTransparent();
            this.labelTimeElapsed = new SolidUtils.GUI.LabelTransparent();
            this.progress = new SolidUtils.GUI.StatusListProgress.StatusList();
            this.statusItem1 = new SolidUtils.GUI.StatusListProgress.StatusItem(this.components);
            this.statusItem2 = new SolidUtils.GUI.StatusListProgress.StatusItem(this.components);
            this.statusItem3 = new SolidUtils.GUI.StatusListProgress.StatusItem(this.components);
            this.statusItem4 = new SolidUtils.GUI.StatusListProgress.StatusItem(this.components);
            this.statusItem5 = new SolidUtils.GUI.StatusListProgress.StatusItem(this.components);
            this.statusItem6 = new SolidUtils.GUI.StatusListProgress.StatusItem(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            this.buttonCancel.BackColor = System.Drawing.Color.AliceBlue;
            this.buttonCancel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonCancel.FlatAppearance.BorderColor = System.Drawing.Color.Lavender;
            this.buttonCancel.FlatAppearance.BorderSize = 2;
            this.buttonCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.buttonCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonCancel.ForeColor = System.Drawing.Color.Crimson;
            this.buttonCancel.Location = new System.Drawing.Point(283, 243);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(102, 35);
            this.buttonCancel.TabIndex = 0;
            this.buttonCancel.Text = "Stop";
            this.buttonCancel.UseVisualStyleBackColor = false;
            this.buttonCancel.Visible = false;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // errorProvider1
            // 
            this.errorProvider1.ContainerControl = this;
            // 
            // labelTransparent1
            // 
            this.labelTransparent1.AutoSize = true;
            this.labelTransparent1.BackColor = System.Drawing.Color.Transparent;
            this.labelTransparent1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.labelTransparent1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.labelTransparent1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(14)))));
            this.labelTransparent1.Location = new System.Drawing.Point(403, 250);
            this.labelTransparent1.Name = "labelTransparent1";
            this.labelTransparent1.Size = new System.Drawing.Size(51, 22);
            this.labelTransparent1.TabIndex = 9;
            this.labelTransparent1.Text = "Stop";
            this.labelTransparent1.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // labelTimeElapsed
            // 
            this.labelTimeElapsed.AutoSize = true;
            this.labelTimeElapsed.BackColor = System.Drawing.Color.Transparent;
            this.labelTimeElapsed.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelTimeElapsed.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(22)))), ((int)(((byte)(94)))), ((int)(((byte)(124)))));
            this.labelTimeElapsed.Location = new System.Drawing.Point(12, 250);
            this.labelTimeElapsed.Name = "labelTimeElapsed";
            this.labelTimeElapsed.Size = new System.Drawing.Size(54, 20);
            this.labelTimeElapsed.TabIndex = 7;
            this.labelTimeElapsed.Text = "00:15";
            // 
            // progress
            // 
            this.progress.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progress.BackColor = System.Drawing.Color.Transparent;
            this.progress.CompleteImage = ((System.Drawing.Image)(resources.GetObject("progress.CompleteImage")));
            this.progress.EmptySpaceToNextItem = 2;
            this.progress.FailedImage = ((System.Drawing.Image)(resources.GetObject("progress.FailedImage")));
            this.progress.Font = new System.Drawing.Font("Verdana", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.progress.ForeColor = System.Drawing.Color.Black;
            this.progress.Items.Add(this.statusItem1);
            this.progress.Items.Add(this.statusItem2);
            this.progress.Items.Add(this.statusItem3);
            this.progress.Items.Add(this.statusItem4);
            this.progress.Items.Add(this.statusItem5);
            this.progress.Items.Add(this.statusItem6);
            this.progress.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(227)))), ((int)(((byte)(247)))), ((int)(((byte)(255)))));
            this.progress.Location = new System.Drawing.Point(12, 12);
            this.progress.Name = "progress";
            this.progress.Pad = 2;
            this.progress.ShowTitle = true;
            this.progress.Size = new System.Drawing.Size(456, 206);
            this.progress.TabIndex = 5;
            this.progress.Text = "statusList1";
            // 
            // statusItem1
            // 
            this.statusItem1.Maximum = 100;
            this.statusItem1.Minimum = 0;
            this.statusItem1.Status = SolidUtils.GUI.StatusListProgress.StatusItem.CurrentStatus.Complete;
            this.statusItem1.Text = "Status message goes here";
            this.statusItem1.Value = 20;
            // 
            // statusItem2
            // 
            this.statusItem2.Maximum = 100;
            this.statusItem2.Minimum = 0;
            this.statusItem2.Status = SolidUtils.GUI.StatusListProgress.StatusItem.CurrentStatus.Failed;
            this.statusItem2.Text = "Status message goes here";
            this.statusItem2.Value = 50;
            // 
            // statusItem3
            // 
            this.statusItem3.Maximum = 100;
            this.statusItem3.Minimum = 0;
            this.statusItem3.Status = SolidUtils.GUI.StatusListProgress.StatusItem.CurrentStatus.Running;
            this.statusItem3.Text = "Status message goes here";
            this.statusItem3.Value = 30;
            // 
            // statusItem4
            // 
            this.statusItem4.CustomEmptySpaceToNextItem = 15;
            this.statusItem4.Maximum = 100;
            this.statusItem4.Minimum = 0;
            this.statusItem4.Status = SolidUtils.GUI.StatusListProgress.StatusItem.CurrentStatus.Running;
            this.statusItem4.Text = "Status message goes here";
            this.statusItem4.Value = 80;
            // 
            // statusItem5
            // 
            this.statusItem5.CustomFontSize = 5F;
            this.statusItem5.CustomPaddingY = 0;
            this.statusItem5.Maximum = 411;
            this.statusItem5.Minimum = 0;
            this.statusItem5.Status = SolidUtils.GUI.StatusListProgress.StatusItem.CurrentStatus.Running;
            this.statusItem5.Text = "Status message goes here";
            this.statusItem5.Value = 77;
            // 
            // statusItem6
            // 
            this.statusItem6.CustomFontSize = 5F;
            this.statusItem6.CustomPaddingY = 0;
            this.statusItem6.Maximum = 100;
            this.statusItem6.Minimum = 0;
            this.statusItem6.Status = SolidUtils.GUI.StatusListProgress.StatusItem.CurrentStatus.Running;
            this.statusItem6.Text = "Status message goes here";
            this.statusItem6.Value = 70;
            // 
            // ColoredProgress
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(480, 300);
            this.Controls.Add(this.labelTransparent1);
            this.Controls.Add(this.labelTimeElapsed);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.buttonCancel);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ColoredProgress";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Operation in progress...";
            this.Shown += new System.EventHandler(this.ColoredProgress_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.ErrorProvider errorProvider1;
        private StatusList progress;
        private StatusItem statusItem1;
        private StatusItem statusItem2;
        private StatusItem statusItem3;
        private LabelTransparent labelTimeElapsed;
        private StatusItem statusItem4;
        private StatusItem statusItem5;
        private LabelTransparent labelTransparent1;
        private StatusItem statusItem6;
    }
}