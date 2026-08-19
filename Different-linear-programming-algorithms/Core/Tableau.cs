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

        public int RowCount => matrix.GetLength(0);
        public int ColCount => matrix.GetLength(1);
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

        // Appends one new constraint row + its slack column to an existing tableau.
        // Used by Branch & Bound (bounding constraint) and Cutting Plane (Gomory cut) —
        // Used Clone() → derive new row → AppendConstraintRow(coeffs, rhs) → DualSimplexSolver.Solve(clone)

        public void AppendConstraintRow(double[] rowCoefficients, double rhs)
        {
            int newRow = RowCount;
            int newSlackCol = ColCount - 1;   // insert just before the RHS column

            var newMatrix = new double[RowCount + 1, ColCount + 1];
            var newBasicVar = new int[BasicVar.Length + 1];
            var newVarNames = new string[VarNames.Length + 1];

            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColCount - 1; j++)
                    newMatrix[i, j] = Matrix[i, j];
                newMatrix[i, newSlackCol] = 0;                    // existing rows: new slack = 0
                newMatrix[i, newSlackCol + 1] = Matrix[i, ColCount - 1];   // shift RHS right
            }

            for (int j = 0; j < rowCoefficients.Length; j++)
                newMatrix[newRow, j] = rowCoefficients[j];
            newMatrix[newRow, newSlackCol] = 1;                   // new row's own slack
            newMatrix[newRow, newSlackCol + 1] = rhs;             // often negative — that's expected

            Array.Copy(BasicVar, newBasicVar, BasicVar.Length);
            newBasicVar[newBasicVar.Length - 1] = newSlackCol;

            Array.Copy(VarNames, newVarNames, VarNames.Length);
            newVarNames[newSlackCol] = $"s{newRow + 1}";

            Matrix = newMatrix;
            BasicVar = newBasicVar;
            VarNames = newVarNames;
        }

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
