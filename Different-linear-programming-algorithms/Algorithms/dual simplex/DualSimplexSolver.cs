using Different_linear_programming_algorithms.Algorithms.Primal_Simplex;
using Different_linear_programming_algorithms.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Different_linear_programming_algorithms.Algorithms.dual_simplex
{
    internal class DualSimplexSolver
    {


        public List<Tableau> Iterations { get; } = new List<Tableau>();
        private const double TOLERANCE = 0.000001;

        public SolvedStatus Solve(Tableau initial)
        {
            Iterations.Clear();
            Tableau current = initial.Clone();
            Iterations.Add(current.Clone());

            while (true)
            {
                int pivotRow = FindPivotRow(current);
                if (pivotRow == -1)
                    break;   // every RHS >= 0 -> feasible again, done

                int pivotColumn = FindPivotColumn(current, pivotRow);
                if (pivotColumn == -1)
                {
                    return new SolvedStatus(SolverStatus.Infeasible,
                        $"Infeasible: row {pivotRow} has a negative RHS with no negative entries to pivot on.",
                        current);
                }

                current.Pivot(pivotRow, pivotColumn);
                Iterations.Add(current.Clone());
            }

            return new SolvedStatus(SolverStatus.Optimal, "Feasibility restored.", current);
        }



        private int FindPivotRow(Tableau t)
        {
            int rhsIndex = t.ColCount - 1;
            int pivotRow = -1;
            double mostNegative = 0;

            for (int i = 1; i < t.RowCount; i++)
            {
                double rhsValue = t.Matrix[i, rhsIndex];
                if (Math.Abs(rhsValue) < TOLERANCE)
                    rhsValue = 0;

                if (rhsValue < mostNegative)
                {
                    mostNegative = rhsValue;
                    pivotRow = i;
                }
            }

            return pivotRow;   // -1 means every RHS is >= 0 -> already feasible, done
        }

        private int FindPivotColumn(Tableau t, int pivotRow)
        {
            int pivotCol = -1;
            double minRatio = double.PositiveInfinity;

            for (int j = 0; j < t.ColCount - 1; j++)   // exclude the RHS column itself
            {
                double rowValue = t.Matrix[pivotRow, j];
                if (NumberHelper.IsNegative(rowValue))
                {
                    double ratio = Math.Abs(t.Matrix[0, j] / rowValue);
                    if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        pivotCol = j;
                    }
                }
            }

            return pivotCol;   // -1 means no negative entries in the pivot row -> infeasible
        }





    }
}
