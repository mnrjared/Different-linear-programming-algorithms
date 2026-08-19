using Different_linear_programming_algorithms.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Different_linear_programming_algorithms.Algorithms.Primal_Simplex
{
    internal class PrimalSimplexSolver
    {
        public List<Tableau> Iterations { get; } = new List<Tableau>();
        public PrimalSimplexSolver() { }
        public SolvedStatus Solve(Tableau initial)
        {
            Iterations.Clear();
            Tableau current = initial.Clone();
            Iterations.Add(current.Clone());   // record the starting tableau too

            while (!IsOptimal(current))
            {
                int enteringCol = FindEnteringColumn(current);
                int leavingRow = FindLeavingRow(current, enteringCol);

                if (leavingRow == -1)
                {
                    return new SolvedStatus(
                        SolverStatus.Unbounded,
                        $"Unbounded: column {current.VarNames[enteringCol]} has no positive entries to pivot on.",
                        current);
                }

                current.Pivot(leavingRow, enteringCol);
                Iterations.Add(current.Clone());
            }

            if (HasNonZeroArtificial(current))
            {
                return new SolvedStatus(
                    SolverStatus.Infeasible,
                    "An artificial variable remains basic with a nonzero value — model is infeasible.",
                    current);
            }

            return new SolvedStatus(
                SolverStatus.Optimal,
                "Optimal solution found.",
                current);
        }

        private bool IsOptimal(Tableau t)
        {
            for (int j = 0; j < t.ColCount - 1; j++)
                if (NumberHelper.IsNegative(t.Matrix[0, j]))
                    return false;
            return true;
        }

        private int FindEnteringColumn(Tableau t)
        {
            int col = -1;
            double mostNegative = 0;
            for (int j = 0; j < t.ColCount - 1; j++)
            {
                if (t.Matrix[0, j] < mostNegative)
                {
                    mostNegative = t.Matrix[0, j];
                    col = j;
                }
            }
            return col;
        }

        private int FindLeavingRow(Tableau t, int enteringCol)
        {
            int row = -1;
            double bestRatio = double.PositiveInfinity;

            for (int i = 1; i < t.RowCount; i++)
            {
                double coeff = t.Matrix[i, enteringCol];
                if (NumberHelper.IsPositive(coeff))
                {
                    double ratio = t.GetRHS(i) / coeff;
                    if (ratio < bestRatio)
                    {
                        bestRatio = ratio;
                        row = i;
                    }
                }
            }
            return row;   // -1 means unbounded
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
