using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;


namespace Different_linear_programming_algorithms.Core
{
    internal class InputParser
    {
        private static double ParseNumber(string token)
        {
            return double.Parse(token, CultureInfo.InvariantCulture);
        }
        public InputParser() { }
        public static LPModel Parse(string[] lines) 
        {
            LPModel model = new LPModel();

            string[] objTokens = lines[0].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            model.IsMax = objTokens[0].Equals("max", StringComparison.OrdinalIgnoreCase);

            model.ObjectiveCoefficients = objTokens.Skip(1).Select(ParseNumber).ToList();

            int numVariables = model.ObjectiveCoefficients.Count;

            for (int i = 1; i < lines.Length - 1; i++)
            {
                string[] tokens = lines[i].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                double[] coefficients = tokens.Take(numVariables).Select(ParseNumber).ToArray();

                string relationToken = tokens[numVariables];

                Relation relation;

                double rhs;

                if (relationToken.StartsWith("<=")) 
                {
                    relation = Relation.LessThanOrEqual;
                    rhs = ParseNumber(relationToken.Substring(2)); 
                }
                else if (relationToken.StartsWith(">=")) 
                {
                    relation = Relation.GreaterThanOrEqual;
                    rhs = ParseNumber(relationToken.Substring(2)); 
                }
                else if (relationToken.StartsWith("=")) 
                { 
                    relation = Relation.Equal; 
                    rhs = ParseNumber(relationToken.Substring(1)); 
                }
                else throw new FormatException($"Unrecognized relation in line: {lines[i]}");

                model.Constraints.Add(new Constraint { Coefficients = coefficients, Relation = relation, RHS = rhs });
            }
                    model.SignRestrictions = lines[lines.Length - 1].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return model;
        } 
    }
}
