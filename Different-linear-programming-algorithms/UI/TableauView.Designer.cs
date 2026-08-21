namespace Different_linear_programming_algorithms.UI
{
    partial class TableauView
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.dgvTableau = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTableau)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(45, 13);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Solution";
            // 
            // dgvTableau
            // 
            this.dgvTableau.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTableau.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTableau.Location = new System.Drawing.Point(0, 13);
            this.dgvTableau.Name = "dgvTableau";
            this.dgvTableau.Size = new System.Drawing.Size(863, 462);
            this.dgvTableau.TabIndex = 1;
            // 
            // TableauView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgvTableau);
            this.Controls.Add(this.lblTitle);
            this.Name = "TableauView";
            this.Size = new System.Drawing.Size(863, 475);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTableau)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.DataGridView dgvTableau;
    }
}
