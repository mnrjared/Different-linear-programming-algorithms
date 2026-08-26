using Different_linear_programming_algorithms.Algorithms.Primal_Simplex;
using Different_linear_programming_algorithms.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Different_linear_programming_algorithms.Algorithms.Duality
{
    internal class DualityReport
    {
        public LPModel PrimalModel { get; set; }
        public LPModel DualModel { get; set; }
        public SolverStatus PrimalStatus { get; set; }
        public SolverStatus DualStatus { get; set; }
        public Solution PrimalSolution { get; set; }
        public Solution DualSolution { get; set; }
        public double DualityGap { get; set; }
        public bool IsStrong { get; set; }
        public bool IsComparable { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("--- Dual Model ---");
            sb.AppendLine(Describe(DualModel, "y"));

            sb.AppendLine($"Primal status : {PrimalStatus}");
            if (PrimalSolution != null)
            {
                sb.AppendLine($"Primal x      : {Values(PrimalSolution)}");
                sb.AppendLine($"Primal z      : {Math.Round(PrimalSolution.ObjectiveValue, 3)}");
            }

            sb.AppendLine($"Dual status   : {DualStatus}");
            if (DualSolution != null)
            {
                sb.AppendLine($"Dual y        : {Values(DualSolution)}");
                sb.AppendLine($"Dual w        : {Math.Round(DualSolution.ObjectiveValue, 3)}");
            }

            if (!IsComparable)
            {
                sb.AppendLine("Verdict       : NOT COMPARABLE - one side is infeasible or unbounded, " +
                              "so there is no gap to measure.");
            }
            else
            {
                sb.AppendLine($"Duality gap   : {Math.Round(DualityGap, 3)}");
                sb.AppendLine(IsStrong
                    ? "Verdict       : STRONG DUALITY - the gap is zero, so z* = w*."
                    : "Verdict       : WEAK DUALITY ONLY - the two objectives bound each other but " +
                      "do not meet, so the gap is non-zero.");
            }
            return sb.ToString();
        }

        private static string Values(Solution s)
        {
            return "(" + string.Join(", ", s.VariableValues.Values.Select(v => Math.Round(v, 3).ToString())) + ")";
        }

        private static string Describe(LPModel m, string variableLetter)
        {
            var sb = new StringBuilder();
            sb.Append(m.IsMax ? "max w = " : "min w = ");
            for (int j = 0; j < m.ObjectiveCoefficients.Count; j++)
                sb.Append($"{m.ObjectiveCoefficients[j]:+0.###;-0.###}{variableLetter}{j + 1} ");
            sb.AppendLine();
            sb.AppendLine("s.t.");
            foreach (var c in m.Constraints)
            {
                sb.Append("     ");
                for (int j = 0; j < c.Coefficients.Length; j++)
                    sb.Append($"{c.Coefficients[j]:+0.###;-0.###}{variableLetter}{j + 1} ");
                sb.AppendLine($"{Symbol(c.Relation)} {c.RHS}");
            }
            sb.AppendLine("     " + string.Join(" ", m.SignRestrictions));
            return sb.ToString();
        }

        private static string Symbol(Relation r)
        {
            if (r == Relation.LessThanOrEqual) return "<=";
            if (r == Relation.GreaterThanOrEqual) return ">=";
            return "=";
        }
    }

    // Applies duality to a programming model, solves the dual independently, and reports
    // whether the pair shows strong or only weak duality.
    //
    // The transformation, for a maximisation primal:
    //   - the dual minimises;
    //   - it has one variable per primal constraint and one constraint per primal variable;
    //   - the constraint matrix is transposed;
    //   - primal right-hand sides become the dual objective coefficients, and primal objective
    //     coefficients become the dual right-hand sides;
    //   - a "<=" primal constraint gives a non-negative dual variable, ">=" gives a non-positive
    //     one, and "=" gives an unrestricted one;
    //   - a non-negative primal variable gives a ">=" dual constraint, an unrestricted primal
    //     variable gives an equality.
    // A minimisation primal is the mirror image: the dual maximises and the constraint
    // directions flip.
    //
    // The "urs" restrictions this produces are now genuinely supported - CanonicalFormBuilder
    // splits a urs variable into x+ and x- columns, so a dual with equality constraints and
    // unrestricted variables solves correctly rather than being silently treated as "+".
    internal class DualityHelper
    {
        public static LPModel BuildDual(LPModel primal)
        {
            int variableCount = primal.ObjectiveCoefficients.Count;
            int constraintCount = primal.Constraints.Count;

            var dual = new LPModel
            {
                // The note in CanonicalFormBuilder is right - this has to be set explicitly.
                IsMax = !primal.IsMax,
                ObjectiveCoefficients = primal.Constraints.Select(c => c.RHS).ToList(),
                Constraints = new List<Constraint>(),
                SignRestrictions = new string[constraintCount]
            };

            // Each dual variable's sign comes from the relation of its primal constraint.
            for (int i = 0; i < constraintCount; i++)
            {
                Relation relation = primal.Constraints[i].Relation;

                if (relation == Relation.Equal)
                    dual.SignRestrictions[i] = "urs";
                else if (primal.IsMax)
                    dual.SignRestrictions[i] = relation == Relation.LessThanOrEqual ? "+" : "-";
                else
                    dual.SignRestrictions[i] = relation == Relation.GreaterThanOrEqual ? "+" : "-";
            }

            // Each dual constraint is built from a column of the primal constraint matrix.
            for (int j = 0; j < variableCount; j++)
            {
                var column = new double[constraintCount];
                for (int i = 0; i < constraintCount; i++)
                {
                    double[] coefficients = primal.Constraints[i].Coefficients;
                    column[i] = j < coefficients.Length ? coefficients[j] : 0.0;
                }

                string restriction = j < primal.SignRestrictions.Length ? primal.SignRestrictions[j] : "+";

                Relation relation;
                if (restriction == "urs")
                    relation = Relation.Equal;
                else
                    relation = primal.IsMax ? Relation.GreaterThanOrEqual : Relation.LessThanOrEqual;

                dual.Constraints.Add(new Constraint(column, relation, primal.ObjectiveCoefficients[j]));
            }

            return dual;
        }

        // Reads the dual solution straight off an optimal primal tableau. The z row entries
        // under the original slack columns are C_bv * B_inverse, which IS the dual optimum.
        // That is why the shadow prices and the dual solution always come out as the same
        // numbers - a useful cross-check on video.
        //
        // Only reads cleanly when every primal constraint kept a real slack column, so a
        // missing one comes back as NaN rather than as a wrong number lifted from whichever
        // column happens to sit at that index.
        public static double[] ReadDualFromPrimalTableau(Tableau optimalPrimal, LPModel primal)
        {
            int constraintCount = primal.Constraints.Count;
            var y = new double[constraintCount];

            for (int i = 0; i < constraintCount; i++)
            {
                int column = ColumnOf(optimalPrimal, $"s{i + 1}");
                y[i] = column == -1 ? double.NaN : optimalPrimal.Matrix[0, column];
            }
            return y;
        }

        // Builds the dual, solves both sides from scratch, and compares. Solving the dual
        // independently rather than lifting it off the primal tableau is the point - two
        // separate routes agreeing is what actually verifies the duality claim.
        public static DualityReport Analyse(LPModel primal)
        {
            var report = new DualityReport
            {
                PrimalModel = primal,
                DualModel = BuildDual(primal)
            };

            SolvedStatus primalRun = SolveModel(primal);
            report.PrimalStatus = primalRun.Status;
            if (primalRun.Status == SolverStatus.Optimal)
                report.PrimalSolution = SolutionExtractor.Extract(primalRun.FinalTableau, primal);

            SolvedStatus dualRun = SolveModel(report.DualModel);
            report.DualStatus = dualRun.Status;
            if (dualRun.Status == SolverStatus.Optimal)
                report.DualSolution = SolutionExtractor.Extract(dualRun.FinalTableau, report.DualModel);

            report.IsComparable = report.PrimalSolution != null && report.DualSolution != null;
            if (report.IsComparable)
            {
                report.DualityGap = Math.Abs(report.PrimalSolution.ObjectiveValue -
                                             report.DualSolution.ObjectiveValue);
                report.IsStrong = report.DualityGap < 1e-6;
            }

            return report;
        }

        private static SolvedStatus SolveModel(LPModel model)
        {
            Tableau canonical = CanonicalFormBuilder.Build(model);
            return new PrimalSimplexSolver().Solve(canonical);
        }

        private static int ColumnOf(Tableau t, string name)
        {
            for (int j = 0; j < t.VarNames.Length; j++)
                if (t.VarNames[j] == name) return j;
            return -1;
        }
    }
}
