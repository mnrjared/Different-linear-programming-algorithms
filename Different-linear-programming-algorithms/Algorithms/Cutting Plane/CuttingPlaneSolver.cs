using Different_linear_programming_algorithms.Algorithms.daul_simplex;
using Different_linear_programming_algorithms.Algorithms.Primal_Simplex;
using Different_linear_programming_algorithms.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Different_linear_programming_algorithms.Algorithms.Cutting_Plane
{
    // One Gomory cut, kept so the video can show where each cut came from rather than
    // just showing tableaux appearing out of nowhere.
    internal class GomoryCut
    {
        public int Number { get; set; }
        public int SourceRow { get; set; }
        public string SourceVariable { get; set; }
        public double SourceValue { get; set; }

        // The cut in readable form: sum of Fractions[j] * x_j >= FractionRhs
        public double[] Fractions { get; set; }
        public double FractionRhs { get; set; }
        public string[] VarNames { get; set; }

        // The same cut as it is actually stored in the tableau, negated so the row arrives
        // with a negative RHS for the dual simplex.
        public double[] RowCoefficients { get; set; }
        public double RowRhs { get; set; }

        public string Describe()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Cut {Number}: taken from row {SourceRow}, where {SourceVariable} = {Math.Round(SourceValue, 3)}");

            var terms = new List<string>();
            for (int j = 0; j < Fractions.Length; j++)
            {
                if (Math.Abs(Fractions[j]) < 1e-6) continue;
                string name = j < VarNames.Length && VarNames[j] != null ? VarNames[j] : $"col{j}";
                terms.Add($"{Math.Round(Fractions[j], 3)}{name}");
            }

            sb.AppendLine(terms.Count == 0
                ? "    (degenerate cut - every fractional part is zero)"
                : $"    {string.Join(" + ", terms)} >= {Math.Round(FractionRhs, 3)}");
            return sb.ToString();
        }
    }

    // Solves an Integer Programming model with the Gomory fractional cutting plane algorithm.
    //
    //   1. Drop the integer restrictions and solve the LP relaxation with the primal simplex.
    //   2. If every variable that has to be an integer already is, that is the IP optimum.
    //   3. Otherwise pick a row whose basic variable came out fractional and read the cut
    //      straight off it. Splitting each coefficient a into floor(a) + f with 0 <= f < 1,
    //      every integer solution must satisfy   sum of f_j * x_j  >=  f_0.
    //      The current fractional optimum does not, which is what makes it a cut.
    //   4. Store it as  -sum f_j x_j + s = -f_0  so it arrives with a negative right-hand
    //      side: dual feasible, primal infeasible - the dual simplex's starting position.
    //   5. Re-optimise with the dual simplex and go back to step 2.
    //
    // Matches PrimalSimplexSolver's shape on purpose: an Iterations list and a SolvedStatus,
    // so the UI can drive this the same way it drives the primal tab.
    internal class CuttingPlaneSolver
    {
        private const int MaxCuts = 50;

        // Deliberately a local constant rather than NumberHelper.Epsilon. Cut generation is the
        // most tolerance-sensitive code in the project - if the shared epsilon is ever set too
        // tight again, this keeps producing sane cuts instead of looping to the cap.
        private const double Tolerance = 1e-6;

        public List<Tableau> Iterations { get; } = new List<Tableau>();
        public List<string> IterationLabels { get; } = new List<string>();
        public List<GomoryCut> Cuts { get; } = new List<GomoryCut>();

        public SolvedStatus Solve(LPModel model)
        {
            Iterations.Clear();
            IterationLabels.Clear();
            Cuts.Clear();

            Tableau initial = CanonicalFormBuilder.Build(model);
            Record(initial, "Canonical form");

            // Step 1 - the LP relaxation. CanonicalFormBuilder has already relaxed the integer
            // restrictions; a bin variable only contributes its x <= 1 upper bound.
            var primal = new PrimalSimplexSolver();
            SolvedStatus relaxation = primal.Solve(initial);
            for (int i = 0; i < primal.Iterations.Count; i++)
                Record(primal.Iterations[i], $"LP relaxation, iteration {i}");

            if (relaxation.Status != SolverStatus.Optimal)
                return relaxation;   // an infeasible or unbounded relaxation ends it here

            Tableau current = relaxation.FinalTableau;

            var integerColumns = GetIntegerColumns(current, model);
            if (integerColumns.Count == 0)
            {
                return new SolvedStatus(
                    SolverStatus.Optimal,
                    "No int or bin variables in this model, so the LP relaxation is already the answer. " +
                    "Cutting Plane has nothing to cut.",
                    current);
            }

            var dual = new DualSimplexSolver();

            // Steps 2 to 5.
            for (int cutNumber = 1; cutNumber <= MaxCuts; cutNumber++)
            {
                int sourceRow = ChooseSourceRow(current, integerColumns);

                if (sourceRow == -1)
                {
                    return new SolvedStatus(
                        SolverStatus.Optimal,
                        $"Integer optimum found after {Cuts.Count} cut(s).",
                        current);
                }

                GomoryCut cut = BuildCut(current, sourceRow, cutNumber);
                Cuts.Add(cut);

                // Clone before appending so the recorded iterations stay intact for the UI.
                current = current.Clone();
                current.AppendConstraintRow(cut.RowCoefficients, cut.RowRhs);
                Record(current, $"Cut {cutNumber} appended");

                SolvedStatus reoptimised = dual.Solve(current);
                for (int i = 1; i < dual.Iterations.Count; i++)
                    Record(dual.Iterations[i], $"Dual simplex after cut {cutNumber}, iteration {i}");

                if (reoptimised.Status != SolverStatus.Optimal)
                {
                    // A cut that kills feasibility means no integer point satisfies the model.
                    return new SolvedStatus(
                        reoptimised.Status,
                        $"After cut {cutNumber}: {reoptimised.Message}",
                        reoptimised.FinalTableau);
                }

                current = reoptimised.FinalTableau;
            }

            return new SolvedStatus(
                SolverStatus.Infeasible,
                $"Stopped after {MaxCuts} cuts without reaching an integer solution. " +
                "Gomory cuts can converge very slowly - try Branch and Bound on this model.",
                current);
        }

        // Which tableau columns hold variables that must come out whole.
        //
        // Columns are located BY NAME, not by index. Since CanonicalFormBuilder started
        // splitting urs variables into an x+ and an x- column, decision variable j is no longer
        // necessarily in column j. Same approach BranchAndBoundSimplexSolver takes.
        private static HashSet<int> GetIntegerColumns(Tableau t, LPModel model)
        {
            var columns = new HashSet<int>();
            for (int j = 0; j < model.ObjectiveCoefficients.Count; j++)
            {
                if (j >= model.SignRestrictions.Length) continue;
                string restriction = model.SignRestrictions[j];
                if (restriction != "int" && restriction != "bin") continue;

                int column = ColumnOf(t, $"x{j + 1}");
                if (column != -1) columns.Add(column);
            }
            return columns;
        }

        // Picks the source row for the next cut: the basic integer variable sitting furthest
        // from a whole number. The most fractional row generally gives the deepest cut, which
        // means fewer iterations before the algorithm closes.
        private static int ChooseSourceRow(Tableau t, HashSet<int> integerColumns)
        {
            int chosen = -1;
            double furthest = Tolerance;

            for (int row = 1; row < t.RowCount; row++)
            {
                int basicColumn = t.BasicVar[row - 1];
                if (!integerColumns.Contains(basicColumn)) continue;

                double value = t.GetRHS(row);
                if (Math.Abs(value - Math.Round(value)) < Tolerance) continue;

                double f = Fraction(value);
                double distance = Math.Min(f, 1.0 - f);
                if (distance > furthest)
                {
                    furthest = distance;
                    chosen = row;
                }
            }
            return chosen;   // -1 means every integer variable is already whole
        }

        // Reads the Gomory cut off a row of the current tableau.
        private static GomoryCut BuildCut(Tableau t, int sourceRow, int cutNumber)
        {
            int variableColumns = t.ColCount - 1;

            var fractions = new double[variableColumns];
            var rowCoefficients = new double[variableColumns];

            for (int j = 0; j < variableColumns; j++)
            {
                fractions[j] = Fraction(t.Matrix[sourceRow, j]);
                rowCoefficients[j] = -fractions[j];   // negated for the dual simplex form
            }

            double fractionRhs = Fraction(t.GetRHS(sourceRow));

            return new GomoryCut
            {
                Number = cutNumber,
                SourceRow = sourceRow,
                SourceVariable = t.VarNames[t.BasicVar[sourceRow - 1]],
                SourceValue = t.GetRHS(sourceRow),
                Fractions = fractions,
                FractionRhs = fractionRhs,
                VarNames = t.VarNames,
                RowCoefficients = rowCoefficients,
                RowRhs = -fractionRhs
            };
        }

        // The fractional part, always in [0, 1). Math.Floor handles negatives correctly: the
        // fractional part of -1.5 is 0.5, not -0.5, which is what the cut needs. Values a hair
        // either side of a whole number snap to zero so floating point noise does not generate
        // a meaningless cut.
        private static double Fraction(double value)
        {
            double f = value - Math.Floor(value);
            if (f < Tolerance || f > 1 - Tolerance) return 0.0;
            return f;
        }

        // Duplicates BranchAndBoundSimplexSolver.FindColumnByName. Worth promoting to a method
        // on Tableau at some point so there is only one copy of it.
        private static int ColumnOf(Tableau t, string name)
        {
            for (int j = 0; j < t.VarNames.Length; j++)
                if (t.VarNames[j] == name) return j;
            return -1;
        }

        private void Record(Tableau t, string label)
        {
            Iterations.Add(t.Clone());
            IterationLabels.Add(label);
        }
    }
}
