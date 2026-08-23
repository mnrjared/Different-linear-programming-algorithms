using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Different_linear_programming_algorithms.Core
{
    // For Person 2 IMPORTANT: bound/incumbent comparisons in BranchAndBoundSimplexSolver.cs file must use the raw internal
    // z-value (tableau.GetRHS(0)) throughout - do NOT call SolutionExtractor.Extract
    // mid-algorithm. The internal solve always maximises (min problems are solved as
    // max w = -z), so "higher internal z is better" holds consistently for every node
    // regardless of whether the original problem was max or min. Only decode the FINAL
    // winning incumbent through SolutionExtractor, once, right before displaying it.
    // See CanonicalFormBuilder.cs and SolutionExtractor.cs for where this convention
    // originates and where it gets corrected.

    // For Person 3 IMPORTANT: the dual model has the OPPOSITE objective type from the primal -
    // dualModel.IsMax = !primalModel.IsMax. This doesn't fall out of reusing the
    // primal's Tableau automatically; it has to be set explicitly when constructing
    // the dual as its own LPModel.
    internal class CanonicalFormBuilder
    {
        private const double M = 1_000_000;

        private static void PriceOutArtificials(Tableau t, int[] basicVariables, int totalCols)
        {
            for (int i = 0; i < basicVariables.Length; i++)
            {
                double zCoeff = t.Matrix[0, basicVariables[i]];
                if (zCoeff == 0) continue;
                for (int j = 0; j < totalCols; j++)
                    t.Matrix[0, j] -= zCoeff * t.Matrix[i + 1, j];
            }
        }

        // Returns the objective coefficients with sign restrictions already applied
        // ("-" restricted columns negated). Used by Build() and by Revised Simplex,
        // so both read the exact same values instead of drifting out of sync.
        public static double[] GetObjectiveCoefficients(LPModel model)
        {
            var objCoeffs = model.ObjectiveCoefficients.ToArray();
            for (int j = 0; j < objCoeffs.Length; j++)
            {
                string restriction = j < model.SignRestrictions.Length ? model.SignRestrictions[j] : "+";
                if (restriction == "-") objCoeffs[j] = -objCoeffs[j];
            }
            return objCoeffs;
        }

        //build initial tableau for algorithms to follow
        public static Tableau Build(LPModel model)
        {
            int numVars = model.ObjectiveCoefficients.Count;

            var objCoeffs = GetObjectiveCoefficients(model);
            var workingConstraints = model.Constraints
                .Select(c => new Constraint((double[])c.Coefficients.Clone(), c.Relation, c.RHS))
                .ToList();

            for (int j = 0; j < numVars; j++)
            {
                string restriction = j < model.SignRestrictions.Length ? model.SignRestrictions[j] : "+";

                if (restriction == "-")
                {
                    foreach (var c in workingConstraints)
                        c.Coefficients[j] = -c.Coefficients[j];
                }
                else if (restriction == "bin")
                {
                    // enforce the relaxation's upper bound; Branch & Bound still has to
                    // force the value to exactly 0 or 1 later
                    var upperBound = new double[numVars];
                    upperBound[j] = 1;
                    workingConstraints.Add(new Constraint(upperBound, Relation.LessThanOrEqual, 1));
                }
            }
            foreach (var c in workingConstraints)
            {
                if (c.RHS < 0)
                {
                    for (int k = 0; k < c.Coefficients.Length; k++)
                        c.Coefficients[k] = -c.Coefficients[k];
                    c.RHS = -c.RHS;
                    c.Relation = c.Relation == Relation.LessThanOrEqual ? Relation.GreaterThanOrEqual
                               : c.Relation == Relation.GreaterThanOrEqual ? Relation.LessThanOrEqual
                               : Relation.Equal;
                }
            }

            // urs variables get split into two non-negative columns: x = x+ - x-
            var positiveCol = new int[numVars];
            var negativeCol = new int[numVars];
            int decisionColumnCount = 0;
            for (int j = 0; j < numVars; j++)
            {
                string restriction = j < model.SignRestrictions.Length ? model.SignRestrictions[j] : "+";
                positiveCol[j] = decisionColumnCount++;
                negativeCol[j] = restriction == "urs" ? decisionColumnCount++ : -1;
            }

            int numConstraints = workingConstraints.Count;
            int slackSurplusCount = workingConstraints.Count(c => c.Relation != Relation.Equal);
            int artificialCount = workingConstraints.Count(c =>
                c.Relation == Relation.GreaterThanOrEqual || c.Relation == Relation.Equal);

            int slackColStart = decisionColumnCount;
            int artificialColStart = decisionColumnCount + slackSurplusCount;
            int totalCols = decisionColumnCount + slackSurplusCount + artificialCount + 1;
            int totalRows = numConstraints + 1;

            double[,] matrix = new double[totalRows, totalCols];
            int[] basicVariables = new int[numConstraints];
            string[] variableNames = new string[totalCols - 1];

            for (int j = 0; j < numVars; j++)
            {
                string restriction = j < model.SignRestrictions.Length ? model.SignRestrictions[j] : "+";
                if (restriction == "urs")
                {
                    variableNames[positiveCol[j]] = $"x{j + 1}+";
                    variableNames[negativeCol[j]] = $"x{j + 1}-";
                }
                else
                {
                    variableNames[positiveCol[j]] = $"x{j + 1}";
                }
            }
            // z-row: negate for max (so Pivot's "optimal when no negatives" rule works uniformly)
            // NOTE FOR EVERYONE: min problems are secretly solved internally as max w = -z.
            // Every raw z-value read from a Tableau (GetRHS(0)) is in this internal convention -
            // only flip it to the real objective value at the point of DISPLAYING a result to
            // the user (see SolutionExtractor). Never flip mid-algorithm - B&B's incumbent
            // comparisons and Sensitivity's calculations must stay in this internal convention
            // throughout, or bound comparisons silently break for min problems.
            for (int j = 0; j < numVars; j++)
            {
                double coeff = model.IsMax ? -objCoeffs[j] : objCoeffs[j];
                matrix[0, positiveCol[j]] = coeff;
                if (negativeCol[j] != -1)
                    matrix[0, negativeCol[j]] = -coeff;
            }

            int slackSurplusIdx = 0;
            int artificialIdx = 0;

            for (int i = 0; i < numConstraints; i++)
            {
                var c = workingConstraints[i];
                for (int j = 0; j < numVars; j++)
                {
                    matrix[i + 1, positiveCol[j]] = c.Coefficients[j];
                    if (negativeCol[j] != -1)
                        matrix[i + 1, negativeCol[j]] = -c.Coefficients[j];
                }

                switch (c.Relation)
                {
                    case Relation.LessThanOrEqual:
                        {
                            int slackCol = slackColStart + slackSurplusIdx++;
                            variableNames[slackCol] = $"s{i + 1}";
                            matrix[i + 1, slackCol] = 1;
                            basicVariables[i] = slackCol;
                            break;
                        }
                    case Relation.GreaterThanOrEqual:
                        {
                            int surplusCol = slackColStart + slackSurplusIdx++;
                            variableNames[surplusCol] = $"e{i + 1}";
                            matrix[i + 1, surplusCol] = -1;
                            int artCol1 = artificialColStart + artificialIdx++;
                            variableNames[artCol1] = $"a{i + 1}";
                            matrix[i + 1, artCol1] = 1;
                            basicVariables[i] = artCol1;
                            matrix[0, artCol1] = M;
                            break;
                        }
                    case Relation.Equal:
                        {
                            int artCol2 = artificialColStart + artificialIdx++;
                            variableNames[artCol2] = $"a{i + 1}";
                            matrix[i + 1, artCol2] = 1;
                            basicVariables[i] = artCol2;
                            matrix[0, artCol2] = M;
                            break;
                        }
                }

                matrix[i + 1, totalCols - 1] = c.RHS;
            }

            var tableau = new Tableau(matrix, basicVariables, variableNames);

            if (artificialCount > 0)
                PriceOutArtificials(tableau, basicVariables, totalCols);

            return tableau;
        }
    }
}