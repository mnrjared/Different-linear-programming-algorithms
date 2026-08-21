using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Different_linear_programming_algorithms.Core
{
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

            int numConstraints = workingConstraints.Count;   // includes any injected bin bounds

            int artificialCount = workingConstraints.Count(c =>
                c.Relation == Relation.GreaterThanOrEqual || c.Relation == Relation.Equal);

            int slackColStart = numVars;
            int artificialColStart = numVars + numConstraints;
            int totalCols = numVars + numConstraints + artificialCount + 1;
            int totalRows = numConstraints + 1;

            double[,] matrix = new double[totalRows, totalCols];
            int[] basicVariables = new int[numConstraints];
            string[] variableNames = new string[totalCols - 1];

            for (int j = 0; j < numVars; j++)
                variableNames[j] = $"x{j + 1}";

            for (int j = 0; j < numVars; j++)
                matrix[0, j] = model.IsMax ? -objCoeffs[j] : objCoeffs[j];

            int artificialIdx = 0;

            for (int i = 0; i < numConstraints; i++)
            {
                var c = workingConstraints[i];
                for (int j = 0; j < numVars; j++)
                    matrix[i + 1, j] = c.Coefficients[j];

                int slackCol = slackColStart + i;

                switch (c.Relation)
                {
                    case Relation.LessThanOrEqual:
                        variableNames[slackCol] = $"s{i + 1}";
                        matrix[i + 1, slackCol] = 1;
                        basicVariables[i] = slackCol;
                        break;

                    case Relation.GreaterThanOrEqual:
                        variableNames[slackCol] = $"e{i + 1}";
                        matrix[i + 1, slackCol] = -1;
                        int artCol1 = artificialColStart + artificialIdx++;
                        variableNames[artCol1] = $"a{i + 1}";
                        matrix[i + 1, artCol1] = 1;
                        basicVariables[i] = artCol1;
                        matrix[0, artCol1] = M;
                        break;

                    case Relation.Equal:
                        int artCol2 = artificialColStart + artificialIdx++;
                        variableNames[artCol2] = $"a{i + 1}";
                        matrix[i + 1, artCol2] = 1;
                        basicVariables[i] = artCol2;
                        matrix[0, artCol2] = M;
                        break;
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