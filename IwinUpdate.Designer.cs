namespace update
{
    partial class IwinUpdate
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IwinUpdate));
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lbComple = new System.Windows.Forms.Label();
            this.ListB01 = new System.Windows.Forms.ListBox();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 96);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "更新进度";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(202, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "正在运行升级程序。。。。。";
            // 
            // lbComple
            // 
            this.lbComple.AutoSize = true;
            this.lbComple.Location = new System.Drawing.Point(19, 143);
            this.lbComple.Name = "lbComple";
            this.lbComple.Size = new System.Drawing.Size(55, 15);
            this.lbComple.TabIndex = 4;
            this.lbComple.Text = "label3";
            // 
            // ListB01
            // 
            this.ListB01.FormattingEnabled = true;
            this.ListB01.ItemHeight = 15;
            this.ListB01.Location = new System.Drawing.Point(25, 176);
            this.ListB01.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ListB01.Name = "ListB01";
            this.ListB01.Size = new System.Drawing.Size(941, 439);
            this.ListB01.TabIndex = 5;
            // 
            // progress
            // 
            this.progress.Location = new System.Drawing.Point(85, 87);
            this.progress.Margin = new System.Windows.Forms.Padding(4);
            this.progress.MarqueeAnimationSpeed = 90;
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(881, 47);
            this.progress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progress.TabIndex = 6;
            // 
            // IwinUpdate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(990, 640);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.ListB01);
            this.Controls.Add(this.lbComple);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "IwinUpdate";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "自动升级程序";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.IwinUpdate_FormClosing);
            this.Load += new System.EventHandler(this.IwinUpdate_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lbComple;
        private System.Windows.Forms.ListBox ListB01;
        private System.Windows.Forms.ProgressBar progress;
    }
}