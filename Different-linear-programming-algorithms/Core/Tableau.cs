using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Different_linear_programming_algorithms.Core
{
    internal class Tableau
    {
        private double[,] matrix;
        private int[] basicVar;
        private string[] varNames;



        public double[,] Matrix { get => matrix; set => matrix = value; }
        public int[] BasicVar { get => basicVar; set => basicVar = value; }
        public string[] VarNames { get => varNames; set => varNames = value; }

        private int RowCount => matrix.GetLength(0);
        private int ColCount => matrix.GetLength(1);
        public Tableau(double[,] matrix, int[] basicVar, string[] varNames)
        {
            Matrix = matrix;
            BasicVar = basicVar;
            VarNames = varNames;
        }
        //for branch and bound to make a copy of a tableau to branch from
        public Tableau Clone()
        {
            double[,] copy = (double[,])matrix.Clone();
            int[] basicCopy = (int[])basicVar.Clone();
            return new Tableau(copy, basicCopy, varNames);
        }

        // The simplex operation. Every iteration in the assignment's output is one call to this.
        public void Pivot(int pivotRow, int pivotCol)
        {
            double pivotValue = matrix[pivotRow, pivotCol];

            // Step 1: scale the pivot row so the pivot element becomes 1
            for (int j = 0; j < ColCount; j++)
                matrix[pivotRow, j] /= pivotValue;

            // Step 2: eliminate the pivot column from every other row
            for (int i = 0; i < RowCount; i++)
            {
                if (i == pivotRow) continue;
                double factor = matrix[i, pivotCol];
                for (int j = 0; j < ColCount; j++)
                    matrix[i, j] -= factor * matrix[pivotRow, j];
            }

            // Step 3: record that pivotCol's variable is now basic in pivotRow
            basicVar[pivotRow - 1] = pivotCol;   // -1 since row 0 is the z-row
        }
        public double GetRHS(int row) => matrix[row, ColCount - 1];

        //  used by OutputWriter to dump each iteration to the results.txt, rounded to 3dp
        public override string ToString()
        {
         
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColCount; j++)
                    sb.Append(System.Math.Round(matrix[i, j], 3)).Append('\t');
                sb.AppendLine();
            }
            return sb.ToString();
        }

    }
}
