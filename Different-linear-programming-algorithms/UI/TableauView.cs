using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Different_linear_programming_algorithms.Core;

namespace Different_linear_programming_algorithms.UI
{
    public partial class TableauView : UserControl
    {
        public TableauView() { InitializeComponent(); }

        internal void DisplayTableau(Tableau tableau, string title = "")
        {
            lblTitle.Text = title;
            dgvTableau.Columns.Clear();
            dgvTableau.Rows.Clear();

            dgvTableau.Columns.Add("rowLabel", "");
            for (int j = 0; j < tableau.VarNames.Length; j++)
                dgvTableau.Columns.Add($"col{j}", tableau.VarNames[j]);
            dgvTableau.Columns.Add("rhs", "RHS");

            var zRow = new object[tableau.VarNames.Length + 2];
            zRow[0] = "z";
            for (int j = 0; j < tableau.ColCount; j++)
                zRow[j + 1] = Math.Round(tableau.Matrix[0, j], 3);
            dgvTableau.Rows.Add(zRow);

            for (int i = 1; i < tableau.RowCount; i++)
            {
                var row = new object[tableau.VarNames.Length + 2];
                row[0] = tableau.VarNames[tableau.BasicVar[i - 1]];
                for (int j = 0; j < tableau.ColCount; j++)
                    row[j + 1] = Math.Round(tableau.Matrix[i, j], 3);
                dgvTableau.Rows.Add(row);
            }
        }
    }
}
