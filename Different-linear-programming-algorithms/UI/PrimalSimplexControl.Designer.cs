namespace Different_linear_programming_algorithms.UI
{
    partial class PrimalSimplexControl
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
            this.cmbAlgorithm = new System.Windows.Forms.ComboBox();
            this.btnSolve = new System.Windows.Forms.Button();
            this.btnPrev = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.dgvSolution = new System.Windows.Forms.DataGridView();
            this.btnSaveResults = new System.Windows.Forms.Button();
            this.tableauView1 = new Different_linear_programming_algorithms.UI.TableauView();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSolution)).BeginInit();
            this.SuspendLayout();
            // 
            // cmbAlgorithm
            // 
            this.cmbAlgorithm.FormattingEnabled = true;
            this.cmbAlgorithm.Location = new System.Drawing.Point(354, 75);
            this.cmbAlgorithm.Name = "cmbAlgorithm";
            this.cmbAlgorithm.Size = new System.Drawing.Size(121, 21);
            this.cmbAlgorithm.TabIndex = 0;
            // 
            // btnSolve
            // 
            this.btnSolve.Location = new System.Drawing.Point(375, 154);
            this.btnSolve.Name = "btnSolve";
            this.btnSolve.Size = new System.Drawing.Size(75, 23);
            this.btnSolve.TabIndex = 1;
            this.btnSolve.Text = "Solve";
            this.btnSolve.UseVisualStyleBackColor = true;
            // 
            // btnPrev
            // 
            this.btnPrev.Location = new System.Drawing.Point(295, 202);
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.Size = new System.Drawing.Size(75, 23);
            this.btnPrev.TabIndex = 2;
            this.btnPrev.Text = "Previous";
            this.btnPrev.UseVisualStyleBackColor = true;
            // 
            // btnNext
            // 
            this.btnNext.Location = new System.Drawing.Point(455, 202);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(75, 23);
            this.btnNext.TabIndex = 3;
            this.btnNext.Text = "Next";
            this.btnNext.UseVisualStyleBackColor = true;
            // 
            // dgvSolution
            // 
            this.dgvSolution.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSolution.Location = new System.Drawing.Point(12, 32);
            this.dgvSolution.Name = "dgvSolution";
            this.dgvSolution.Size = new System.Drawing.Size(277, 217);
            this.dgvSolution.TabIndex = 4;
            // 
            // btnSaveResults
            // 
            this.btnSaveResults.Location = new System.Drawing.Point(375, 249);
            this.btnSaveResults.Name = "btnSaveResults";
            this.btnSaveResults.Size = new System.Drawing.Size(75, 23);
            this.btnSaveResults.TabIndex = 5;
            this.btnSaveResults.Text = "Save";
            this.btnSaveResults.UseVisualStyleBackColor = true;
            // 
            // tableauView1
            // 
            this.tableauView1.Location = new System.Drawing.Point(12, 287);
            this.tableauView1.Name = "tableauView1";
            this.tableauView1.Size = new System.Drawing.Size(558, 249);
            this.tableauView1.TabIndex = 6;
            // 
            // PrimalSimplexControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableauView1);
            this.Controls.Add(this.btnSaveResults);
            this.Controls.Add(this.dgvSolution);
            this.Controls.Add(this.btnNext);
            this.Controls.Add(this.btnPrev);
            this.Controls.Add(this.btnSolve);
            this.Controls.Add(this.cmbAlgorithm);
            this.Name = "PrimalSimplexControl";
            this.Size = new System.Drawing.Size(1055, 593);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSolution)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbAlgorithm;
        private System.Windows.Forms.Button btnSolve;
        private System.Windows.Forms.Button btnPrev;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.DataGridView dgvSolution;
        private System.Windows.Forms.Button btnSaveResults;
        private TableauView tableauView1;
    }
}
