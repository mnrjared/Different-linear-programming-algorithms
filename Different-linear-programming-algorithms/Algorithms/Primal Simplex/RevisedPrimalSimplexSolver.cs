using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Different_linear_programming_algorithms.Core;

namespace Different_linear_programming_algorithms.Algorithms.Primal_Simplex
{
    internal class RevisedIteration
    {
        public double[,] BInverse { get; set; }
        public double[] Y { get; set; }
        public double[] ReducedCosts { get; set; }
        public double[] CurrentRHS { get; set; }
        public int[] Basis { get; set; }
        public int EnteringColumn { get; set; } = -1;
        public int LeavingRow { get; set; } = -1;
    }

    internal class RevisedPrimalSimplexSolver
    {
        private const double M = 1_000_000;
        public List<RevisedIteration> Iterations { get; } = new List<RevisedIteration>();

        public SolvedStatus Solve(LPModel model)
        {
            Iterations.Clear();
            Tableau initial = CanonicalFormBuilder.Build(model);

            int m = initial.RowCount - 1;
            int n = initial.ColCount - 1;

            double[,] A = new double[m, n];
            double[] b = new double[m];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++) A[i, j] = initial.Matrix[i + 1, j];
                b[i] = initial.Matrix[i + 1, n];
            }

            double[] objCoeffs = CanonicalFormBuilder.GetObjectiveCoefficients(model);
            double[] cost = new double[n];
            for (int j = 0; j < n; j++)
            {
                if (j < objCoeffs.Length)
                    cost[j] = model.IsMax ? objCoeffs[j] : -objCoeffs[j];
                else if (initial.VarNames[j].StartsWith("a"))
                    cost[j] = -M;
                else
                    cost[j] = 0;
            }

            int[] basis = (int[])initial.BasicVar.Clone();
            double[,] bInverse = Identity(m);

            while (true)
            {
                double[] y = MultiplyRowVectorByMatrix(BasicCosts(cost, basis), bInverse);
                double[] reducedCosts = new double[n];
                for (int j = 0; j < n; j++)
                    reducedCosts[j] = DotColumn(y, A, j) - cost[j];

                double[] currentRHS = MultiplyMatrixByVector(bInverse, b);
                int enteringCol = FindMostNegative(reducedCosts);

                var snapshot = new RevisedIteration
                {
                    BInverse = (double[,])bInverse.Clone(),
                    Y = y,
                    ReducedCosts = reducedCosts,
                    CurrentRHS = currentRHS,
                    Basis = (int[])basis.Clone(),
                    EnteringColumn = enteringCol
                };

                if (enteringCol == -1) { Iterations.Add(snapshot); break; }

                double[] d = MultiplyMatrixByVector(bInverse, GetColumn(A, enteringCol, m));
                int leavingRow = FindLeavingRow(d, currentRHS);
                snapshot.LeavingRow = leavingRow;
                Iterations.Add(snapshot);

                if (leavingRow == -1)
                    return new SolvedStatus(SolverStatus.Unbounded,
                        $"Unbounded: column {initial.VarNames[enteringCol]} has no positive entries under the current basis.", null);

                UpdateBInverse(bInverse, d, leavingRow);
                basis[leavingRow] = enteringCol;
            }

            var last = Iterations[Iterations.Count - 1];
            Tableau finalTableau = RevisedTableauAdapter.ToDisplayTableau(last, initial);

            if (HasNonZeroArtificial(finalTableau))
                return new SolvedStatus(SolverStatus.Infeasible,
                    "An artificial variable remains basic with a nonzero value — model is infeasible.", finalTableau);

            return new SolvedStatus(SolverStatus.Optimal, "Optimal solution found.", finalTableau);
        }

        private double[,] Identity(int size)
        {
            var result = new double[size, size];
            for (int i = 0; i < size; i++) result[i, i] = 1;
            return result;
        }

        private double[] BasicCosts(double[] cost, int[] basis)
        {
            var result = new double[basis.Length];
            for (int i = 0; i < basis.Length; i++) result[i] = cost[basis[i]];
            return result;
        }

        private double[] MultiplyRowVectorByMatrix(double[] rowVector, double[,] matrix)
        {
            int cols = matrix.GetLength(1);
            var result = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                double sum = 0;
                for (int i = 0; i < rowVector.Length; i++) sum += rowVector[i] * matrix[i, j];
                result[j] = sum;
            }
            return result;
        }

        private double[] MultiplyMatrixByVector(double[,] matrix, double[] vector)
        {
            int rows = matrix.GetLength(0), cols = matrix.GetLength(1);
            var result = new double[rows];
            for (int i = 0; i < rows; i++)
            {
                double sum = 0;
                for (int j = 0; j < cols; j++) sum += matrix[i, j] * vector[j];
                result[i] = sum;
            }
            return result;
        }

        private double[] GetColumn(double[,] A, int col, int rows)
        {
            var result = new double[rows];
            for (int i = 0; i < rows; i++) result[i] = A[i, col];
            return result;
        }

        private double DotColumn(double[] y, double[,] A, int col)
        {
            double sum = 0;
            for (int i = 0; i < y.Length; i++) sum += y[i] * A[i, col];
            return sum;
        }

        private int FindMostNegative(double[] values)
        {
            int idx = -1;
            double worst = 0;
            for (int j = 0; j < values.Length; j++)
                if (NumberHelper.IsNegative(values[j]) && values[j] < worst) { worst = values[j]; idx = j; }
            return idx;
        }

        private int FindLeavingRow(double[] d, double[] currentRHS)
        {
            int row = -1;
            double bestRatio = double.PositiveInfinity;
            for (int i = 0; i < d.Length; i++)
                if (NumberHelper.IsPositive(d[i]))
                {
                    double ratio = currentRHS[i] / d[i];
                    if (ratio < bestRatio) { bestRatio = ratio; row = i; }
                }
            return row;
        }

        private void UpdateBInverse(double[,] bInverse, double[] d, int leavingRow)
        {
            int m = d.Length;
            var eta = new double[m];
            for (int i = 0; i < m; i++)
                eta[i] = (i == leavingRow) ? 1.0 / d[leavingRow] : -d[i] / d[leavingRow];

            var updated = new double[m, m];
            for (int col = 0; col < m; col++)
            {
                double pivotRowValue = bInverse[leavingRow, col];
                for (int row = 0; row < m; row++)
                    updated[row, col] = (row == leavingRow)
                        ? eta[row] * pivotRowValue
                        : bInverse[row, col] + eta[row] * pivotRowValue;
            }
            Array.Copy(updated, bInverse, updated.Length);
        }

        private bool HasNonZeroArtificial(Tableau t)
        {
            for (int i = 0; i < t.BasicVar.Length; i++)
            {
                string name = t.VarNames[t.BasicVar[i]];
                if (name.StartsWith("a") && !NumberHelper.IsZero(t.GetRHS(i + 1)))
                    return true;
            }
            return false;
        }
    }
}
