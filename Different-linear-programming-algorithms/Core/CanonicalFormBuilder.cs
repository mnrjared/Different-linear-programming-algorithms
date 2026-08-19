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

        //build intial tableau for algorithms to follow
        //if building for daul simplex add code to skip the columns with artificail varaibles

        public static Tableau Build(LPModel model) 
        {
            int numVars = model.ObjectiveCoefficients.Count;
            int numConstraints = model.Constraints.Count;

            int artificialCount = model.Constraints.Count(c =>
                c.Relation == Relation.GreaterThanOrEqual || c.Relation == Relation.Equal);

            int slackColStart = numVars;
            int artificialColStart = numVars + numConstraints;
            int totalCols = numVars + numConstraints + artificialCount + 1;   // +1 = RHS
            int totalRows = numConstraints + 1;                              // +1 = z-row

            double[,] matrix = new double[totalRows, totalCols];
            int[] basicVariables = new int[numConstraints];
            string[] variableNames = new string[totalCols - 1];

            for (int j = 0; j < numVars; j++)
                variableNames[j] = $"x{j + 1}";

            // z-row: negate for max (so Pivot's "optimal when no negatives" rule works uniformly)
            for (int j = 0; j < numVars; j++)
                matrix[0, j] = model.IsMax ? -model.ObjectiveCoefficients[j] : model.ObjectiveCoefficients[j];

            int artificialIdx = 0;

            for (int i = 0; i < numConstraints; i++)
            {
                var c = model.Constraints[i];
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
