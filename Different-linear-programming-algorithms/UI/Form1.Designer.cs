namespace Different_linear_programming_algorithms
{
    partial class Form1
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
            this.Upload = new System.Windows.Forms.Button();
            this.lblFileName = new System.Windows.Forms.Label();
            this.TabControl = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.primalSimplexControl1 = new Different_linear_programming_algorithms.UI.PrimalSimplexControl();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.branchAndBoundControl1 = new Different_linear_programming_algorithms.UI.BranchAndBoundControl();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.cuttingPlaneControl1 = new Different_linear_programming_algorithms.UI.CuttingPlaneControl();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.knapsackControl1 = new Different_linear_programming_algorithms.UI.KnapsackControl();
            this.TabControl.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.SuspendLayout();
            // 
            // Upload
            // 
            this.Upload.Location = new System.Drawing.Point(620, 672);
            this.Upload.Name = "Upload";
            this.Upload.Size = new System.Drawing.Size(75, 23);
            this.Upload.TabIndex = 0;
            this.Upload.Text = "Upload";
            this.Upload.UseVisualStyleBackColor = true;
            this.Upload.Click += new System.EventHandler(this.Upload_Click);
            // 
            // lblFileName
            // 
            this.lblFileName.AutoSize = true;
            this.lblFileName.Location = new System.Drawing.Point(623, 656);
            this.lblFileName.Name = "lblFileName";
            this.lblFileName.Size = new System.Drawing.Size(72, 13);
            this.lblFileName.TabIndex = 1;
            this.lblFileName.Text = "No file loaded";
            // 
            // TabControl
            // 
            this.TabControl.Controls.Add(this.tabPage1);
            this.TabControl.Controls.Add(this.tabPage2);
            this.TabControl.Controls.Add(this.tabPage3);
            this.TabControl.Controls.Add(this.tabPage4);
            this.TabControl.Location = new System.Drawing.Point(12, 12);
            this.TabControl.Name = "TabControl";
            this.TabControl.SelectedIndex = 0;
            this.TabControl.Size = new System.Drawing.Size(1316, 614);
            this.TabControl.TabIndex = 2;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.primalSimplexControl1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1308, 588);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Primal Simplex";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // primalSimplexControl1
            // 
            this.primalSimplexControl1.Location = new System.Drawing.Point(-4, 0);
            this.primalSimplexControl1.Margin = new System.Windows.Forms.Padding(4);
            this.primalSimplexControl1.Name = "primalSimplexControl1";
            this.primalSimplexControl1.Size = new System.Drawing.Size(1315, 593);
            this.primalSimplexControl1.TabIndex = 0;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.branchAndBoundControl1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1308, 588);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Branch and Bound";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // branchAndBoundControl1
            // 
            this.branchAndBoundControl1.Location = new System.Drawing.Point(-4, -3);
            this.branchAndBoundControl1.Name = "branchAndBoundControl1";
            this.branchAndBoundControl1.Size = new System.Drawing.Size(1312, 585);
            this.branchAndBoundControl1.TabIndex = 0;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.cuttingPlaneControl1);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(1308, 588);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Cutting Plane/Duality";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // cuttingPlaneControl1
            // 
            this.cuttingPlaneControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cuttingPlaneControl1.Location = new System.Drawing.Point(3, 3);
            this.cuttingPlaneControl1.Margin = new System.Windows.Forms.Padding(2);
            this.cuttingPlaneControl1.Name = "cuttingPlaneControl1";
            this.cuttingPlaneControl1.Size = new System.Drawing.Size(1302, 582);
            this.cuttingPlaneControl1.TabIndex = 0;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.knapsackControl1);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(1308, 588);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Knapsack";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // knapsackControl1
            // 
            this.knapsackControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.knapsackControl1.Location = new System.Drawing.Point(0, 0);
            this.knapsackControl1.Name = "knapsackControl1";
            this.knapsackControl1.Size = new System.Drawing.Size(1308, 588);
            this.knapsackControl1.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1340, 744);
            this.Controls.Add(this.TabControl);
            this.Controls.Add(this.lblFileName);
            this.Controls.Add(this.Upload);
            this.Name = "Form1";
            this.Text = "Form1";
            this.TabControl.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button Upload;
        private System.Windows.Forms.Label lblFileName;
        private System.Windows.Forms.TabControl TabControl;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private UI.PrimalSimplexControl primalSimplexControl1;
        private System.Windows.Forms.TabPage tabPage3;
        private UI.CuttingPlaneControl cuttingPlaneControl1;
        private System.Windows.Forms.TabPage tabPage4;
        private UI.BranchAndBoundControl branchAndBoundControl1;
        private UI.KnapsackControl knapsackControl1;
    }
}

