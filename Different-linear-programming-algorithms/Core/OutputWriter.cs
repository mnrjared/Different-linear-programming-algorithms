using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace Different_linear_programming_algorithms.Core
{
    internal class OutputWriter
    {
        private readonly StringBuilder _sb = new StringBuilder();

        public void WriteHeader(string title)
        {
            _sb.AppendLine(new string('=', 50));
            _sb.AppendLine(title);
            _sb.AppendLine(new string('=', 50));
        }

        public void WriteCanonicalForm(LPModel model)
        {
            _sb.AppendLine("--- Canonical Form ---");
            _sb.Append(model.IsMax ? "max z = " : "min z = ");
            for (int j = 0; j < model.ObjectiveCoefficients.Count; j++)
                _sb.Append($"{model.ObjectiveCoefficients[j]:+0.###;-0.###}x{j + 1} ");
            _sb.AppendLine();

            for (int i = 0; i < model.Constraints.Count; i++)
            {
                var c = model.Constraints[i];
                for (int j = 0; j < c.Coefficients.Length; j++)
                    _sb.Append($"{c.Coefficients[j]:+0.###;-0.###}x{j + 1} ");
                _sb.AppendLine($"{c.Relation} {c.RHS}");
            }
            _sb.AppendLine();
        }

        public void WriteTableau(int iterationNumber, Tableau tableau)
        {
            _sb.AppendLine($"--- Iteration {iterationNumber} ---");
            _sb.AppendLine(tableau.ToString());
        }

        public void WriteSolution(Solution solution)
        {
            _sb.AppendLine("--- Optimal Solution ---");
            foreach (var kvp in solution.VariableValues)
                _sb.AppendLine($"{kvp.Key} = {kvp.Value}");
            _sb.AppendLine($"z = {solution.ObjectiveValue}");
            _sb.AppendLine();
        }

        public void WriteError(string message)
        {
            _sb.AppendLine($"ERROR: {message}");
        }

        // Free-form text block. Needed by the Duality report, which has no tableau to write,
        // and useful for Branch and Bound's node log.
        public void WriteText(string text) => _sb.AppendLine(text);

        public string GetContent() => _sb.ToString();

        public void SaveToFile(string path) => File.WriteAllText(path, _sb.ToString());
    }
}
