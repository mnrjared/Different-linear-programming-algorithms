namespace Different_linear_programming_algorithms.UI
{
    partial class BranchAndBoundControl
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
            this.btnSolve = new System.Windows.Forms.Button();
            this.treeViewNodes = new System.Windows.Forms.TreeView();
            this.tableauView1 = new Different_linear_programming_algorithms.UI.TableauView();
            this.dgvSolution = new System.Windows.Forms.DataGridView();
            this.btnSaveResults = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSolution)).BeginInit();
            this.SuspendLayout();
            // 
            // btnSolve
            // 
            this.btnSolve.Location = new System.Drawing.Point(543, 176);
            this.btnSolve.Name = "btnSolve";
            this.btnSolve.Size = new System.Drawing.Size(75, 23);
            this.btnSolve.TabIndex = 0;
            this.btnSolve.Text = "Solve";
            this.btnSolve.UseVisualStyleBackColor = true;
            // 
            // treeViewNodes
            // 
            this.treeViewNodes.Location = new System.Drawing.Point(32, 20);
            this.treeViewNodes.Name = "treeViewNodes";
            this.treeViewNodes.Size = new System.Drawing.Size(201, 179);
            this.treeViewNodes.TabIndex = 1;
            // 
            // tableauView1
            // 
            this.tableauView1.Location = new System.Drawing.Point(32, 208);
            this.tableauView1.Name = "tableauView1";
            this.tableauView1.Size = new System.Drawing.Size(896, 357);
            this.tableauView1.TabIndex = 2;
            // 
            // dgvSolution
            // 
            this.dgvSolution.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSolution.Location = new System.Drawing.Point(260, 20);
            this.dgvSolution.Name = "dgvSolution";
            this.dgvSolution.Size = new System.Drawing.Size(240, 150);
            this.dgvSolution.TabIndex = 3;
            // 
            // btnSaveResults
            // 
            this.btnSaveResults.Location = new System.Drawing.Point(709, 179);
            this.btnSaveResults.Name = "btnSaveResults";
            this.btnSaveResults.Size = new System.Drawing.Size(75, 23);
            this.btnSaveResults.TabIndex = 4;
            this.btnSaveResults.Text = "Save";
            this.btnSaveResults.UseVisualStyleBackColor = true;
            // 
            // BranchAndBoundControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnSaveResults);
            this.Controls.Add(this.dgvSolution);
            this.Controls.Add(this.tableauView1);
            this.Controls.Add(this.treeViewNodes);
            this.Controls.Add(this.btnSolve);
            this.Name = "BranchAndBoundControl";
            this.Size = new System.Drawing.Size(1132, 585);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSolution)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnSolve;
        private System.Windows.Forms.TreeView treeViewNodes;
        private TableauView tableauView1;
        private System.Windows.Forms.DataGridView dgvSolution;
        private System.Windows.Forms.Button btnSaveResults;
    }
}
