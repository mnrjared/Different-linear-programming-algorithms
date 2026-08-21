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

            for (int j = 0; j < numVars; j++)
            {
                double raw = 0;
                for (int row = 0; row < finalTableau.BasicVar.Length; row++)
                    if (finalTableau.BasicVar[row] == j)
                        raw = Math.Round(finalTableau.GetRHS(row + 1), 3);

                string restriction = j < model.SignRestrictions.Length ? model.SignRestrictions[j] : "+";
                values[finalTableau.VarNames[j]] = restriction == "-" ? -raw : raw;
            }
            // This is the ONLY place this sign correction should happen for a given result —
            // if you're writing Sensitivity or B&B code that reports a z-value, call through
            // here (or apply this exact same "model.IsMax ? raw : -raw" pattern) at your own
            // final display boundary, rather than flipping earlier and risking a double-flip
            // or an inconsistent comparison somewhere upstream.
            double rawZ = finalTableau.GetRHS(0);
            double objective = model.IsMax ? rawZ : -rawZ;   // internal solve always maximises

            return new Solution
            {
                VariableValues = values,
                ObjectiveValue = Math.Round(objective, 3)
            };
        }
    }
}
