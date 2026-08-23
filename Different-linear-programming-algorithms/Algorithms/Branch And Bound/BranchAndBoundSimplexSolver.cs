using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Different_linear_programming_algorithms.Core;
using Different_linear_programming_algorithms.Algorithms.Primal_Simplex;
using Different_linear_programming_algorithms.Algorithms.daul_simplex;

namespace Different_linear_programming_algorithms.Algorithms.BranchAndBound
{
    internal class BnBNode
    {
        public Tableau Tableau { get; set; }
        public double Bound { get; set; }   // internal (always-maximise) z-value - higher is always better,
                                            // regardless of whether the original model was max or min
        public int Depth { get; set; }
        public string BranchDescription { get; set; }
    }

    internal class BranchAndBoundSimplexSolver
    {
        private const int MaxNodes = 500;
        private const int MaxDepth = 30;

        public List<BnBNode> ExploredNodes { get; } = new List<BnBNode>();
        public int TotalNodes { get; private set; }
        public int PrunedNodes { get; private set; }

        public SolvedStatus Solve(LPModel model)
        {
            ExploredNodes.Clear();
            TotalNodes = 0;
            PrunedNodes = 0;

            Tableau rootTableau = CanonicalFormBuilder.Build(model);
            var rootSolver = new PrimalSimplexSolver();
            SolvedStatus rootResult = rootSolver.Solve(rootTableau);

            if (rootResult.Status != SolverStatus.Optimal)
                return rootResult;   // infeasible/unbounded relaxation -> the IP is too

            if (IsIntegerFeasible(rootResult.FinalTableau, model))
                return rootResult;   // relaxation is already integer -> nothing to branch on

            var root = new BnBNode
            {
                Tableau = rootResult.FinalTableau,
                Bound = rootResult.FinalTableau.GetRHS(0),
                Depth = 0,
                BranchDescription = "Root relaxation"
            };

            var stack = new Stack<BnBNode>();   // stack -> depth-first backtracking, per the assignment
            stack.Push(root);

            Tableau bestTableau = null;
            double bestBound = double.NegativeInfinity;

            while (stack.Count > 0 && TotalNodes < MaxNodes)
            {
                var node = stack.Pop();
                TotalNodes++;
                ExploredNodes.Add(node);

                if (node.Bound <= bestBound || node.Depth > MaxDepth)
                {
                    PrunedNodes++;
                    continue;   // fathomed: can't beat the current best
                }

                if (IsIntegerFeasible(node.Tableau, model))
                {
                    if (node.Bound > bestBound)
                    {
                        bestBound = node.Bound;
                        bestTableau = node.Tableau;
                    }
                    continue;   // fathomed: integer-feasible candidate found
                }

                int branchCol = FindMostFractionalColumn(node.Tableau, model, out int branchRow, out double branchValue);
                if (branchCol == -1)
                {
                    PrunedNodes++;
                    continue;
                }

                double floorVal = Math.Floor(branchValue);
                double ceilVal = Math.Ceiling(branchValue);
                string varName = node.Tableau.VarNames[branchCol];

                TryCreateChild(node, branchCol, branchRow, floorVal, isLessThanOrEqual: true,
                    $"{varName} <= {floorVal}", stack);
                TryCreateChild(node, branchCol, branchRow, ceilVal, isLessThanOrEqual: false,
                    $"{varName} >= {ceilVal}", stack);
            }

            if (bestTableau == null)
                return new SolvedStatus(SolverStatus.Infeasible,
                    "No integer-feasible solution found within the search limits.", null);

            return new SolvedStatus(SolverStatus.Optimal, "Optimal integer solution found.", bestTableau);
        }

        private void TryCreateChild(BnBNode parent, int col, int row, double bound, bool isLessThanOrEqual,
            string description, Stack<BnBNode> stack)
        {
            Tableau childTableau = parent.Tableau.Clone();
            double[] rowCoeffs = BuildBranchRow(parent.Tableau, col, row, bound, isLessThanOrEqual, out double rhs);
            childTableau.AppendConstraintRow(rowCoeffs, rhs);

            var dualSolver = new DualSimplexSolver();  
            SolvedStatus result = dualSolver.Solve(childTableau);

            if (result.Status != SolverStatus.Optimal)
                return;   // infeasible/unbounded branch -> fathomed, just don't push it

            stack.Push(new BnBNode
            {
                Tableau = result.FinalTableau,
                Bound = result.FinalTableau.GetRHS(0),
                Depth = parent.Depth + 1,
                BranchDescription = description
            });
        }

        // Derives the new row by substituting the branching column's CURRENT row -
        // required because the column is basic (only basic variables can be fractional),
        // so a raw unit-vector row would conflict with the existing basic-variable
        // invariant. Same substitution a Gomory cut uses.
        private double[] BuildBranchRow(Tableau t, int col, int row, double bound, bool isLessThanOrEqual, out double rhs)
        {
            var newRow = new double[t.ColCount - 1];
            double sign = isLessThanOrEqual ? -1 : 1;

            for (int k = 0; k < newRow.Length; k++)
                newRow[k] = k == col ? 0 : sign * t.Matrix[row, k];

            rhs = isLessThanOrEqual
                ? bound - t.GetRHS(row)
                : t.GetRHS(row) - bound;

            return newRow;
        }

        private bool IsIntegerFeasible(Tableau t, LPModel model)
        {
            for (int j = 0; j < model.ObjectiveCoefficients.Count; j++)
            {
                string restriction = j < model.SignRestrictions.Length ? model.SignRestrictions[j] : "+";
                if (restriction != "int" && restriction != "bin") continue;

                int col = FindColumnByName(t, $"x{j + 1}");
                if (col == -1) continue;

                double value = GetColumnValue(t, col, out _);
                if (!NumberHelper.IsInteger(value))
                    return false;
            }
            return true;
        }

        private int FindMostFractionalColumn(Tableau t, LPModel model, out int row, out double value)
        {
            int bestCol = -1, bestRow = -1;
            double bestValue = 0, bestFraction = -1;

            for (int j = 0; j < model.ObjectiveCoefficients.Count; j++)
            {
                string restriction = j < model.SignRestrictions.Length ? model.SignRestrictions[j] : "+";
                if (restriction != "int" && restriction != "bin") continue;

                int col = FindColumnByName(t, $"x{j + 1}");
                if (col == -1) continue;

                double val = GetColumnValue(t, col, out int r);
                if (r == -1) continue;   // non-basic -> already exactly 0, never fractional

                double fraction = Math.Abs(val - Math.Round(val));
                if (fraction > 1e-6 && fraction > bestFraction)
                {
                    bestFraction = fraction;
                    bestCol = col;
                    bestRow = r;
                    bestValue = val;
                }
            }

            row = bestRow;
            value = bestValue;
            return bestCol;
        }

        private int FindColumnByName(Tableau t, string name)
        {
            for (int j = 0; j < t.VarNames.Length; j++)
                if (t.VarNames[j] == name) return j;
            return -1;
        }

        private double GetColumnValue(Tableau t, int col, out int row)
        {
            for (int i = 0; i < t.BasicVar.Length; i++)
            {
                if (t.BasicVar[i] == col)
                {
                    row = i + 1;
                    return t.GetRHS(i + 1);
                }
            }
            row = -1;
            return 0;
        }
    }
}
