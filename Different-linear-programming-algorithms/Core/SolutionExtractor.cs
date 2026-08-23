using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Different_linear_programming_algorithms.Core
{
    internal class SolutionExtractor
    {
        public static Solution Extract(Tableau finalTableau, LPModel model)
        {
            int numVars = model.ObjectiveCoefficients.Count;
            var values = new Dictionary<string, double>();

            var positiveCol = new int[numVars];
            var negativeCol = new int[numVars];
            int decisionColumnCount = 0;
            for (int j = 0; j < numVars; j++)
            {
                string r = j < model.SignRestrictions.Length ? model.SignRestrictions[j] : "+";
                positiveCol[j] = decisionColumnCount++;
                negativeCol[j] = r == "urs" ? decisionColumnCount++ : -1;
            }

            for (int j = 0; j < numVars; j++)
            {
                string restriction = j < model.SignRestrictions.Length ? model.SignRestrictions[j] : "+";
                double rawPositive = ReadColumnValue(finalTableau, positiveCol[j]);

                double value;
                if (restriction == "urs")
                    value = rawPositive - ReadColumnValue(finalTableau, negativeCol[j]);
                else if (restriction == "-")
                    value = -rawPositive;
                else
                    value = rawPositive;

                values[$"x{j + 1}"] = Math.Round(value, 3);
            }

            double rawZ = finalTableau.GetRHS(0);
            double objective = model.IsMax ? rawZ : -rawZ;

            return new Solution
            {
                VariableValues = values,
                ObjectiveValue = Math.Round(objective, 3)
            };
        }

        private static double ReadColumnValue(Tableau t, int col)
        {
            for (int row = 0; row < t.BasicVar.Length; row++)
                if (t.BasicVar[row] == col)
                    return Math.Round(t.GetRHS(row + 1), 3);
            return 0;
        }
    }
}