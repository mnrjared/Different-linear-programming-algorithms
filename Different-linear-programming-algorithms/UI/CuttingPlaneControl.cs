using Different_linear_programming_algorithms.Algorithms.Cutting_Plane;
using Different_linear_programming_algorithms.Algorithms.Duality;
using Different_linear_programming_algorithms.Algorithms.Sensitivity;
using Different_linear_programming_algorithms.Algorithms.Primal_Simplex;
using Different_linear_programming_algorithms.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Different_linear_programming_algorithms.UI
{
    // Person 3's tab: Cutting Plane, Duality, and all of the sensitivity operations.
    //
    // Same shape as PrimalSimplexControl - implements IAlgorithmTab so Form1's Upload_Click
    // pushes the parsed model in automatically. Controls are built in code rather than through
    // a .Designer.cs file, so there is one file to maintain and nothing to regenerate.
    public partial class CuttingPlaneControl : UserControl, IAlgorithmTab
    {
        // Index positions must match the order items are added to cmbOperation.
        private const int OpCuttingPlane = 0;
        private const int OpDuality = 1;
        private const int OpShadowPrices = 2;
        private const int OpAllRanges = 3;
        private const int OpNonBasicRange = 4;
        private const int OpNonBasicChange = 5;
        private const int OpBasicRange = 6;
        private const int OpBasicChange = 7;
        private const int OpRhsRange = 8;
        private const int OpRhsChange = 9;
        private const int OpColumnRange = 10;
        private const int OpColumnChange = 11;
        private const int OpAddActivity = 12;
        private const int OpAddConstraint = 13;

        private LPModel _currentModel;
        private List<Tableau> _iterations;
        private List<string> _labels;
        private int _index;
        private OutputWriter _writer;

        private ComboBox cmbOperation;
        private Button btnRun;
        private Button btnPrev;
        private Button btnNext;
        private Button btnSave;
        private TableauView tableauView;
        private TextBox txtNarrative;

        public CuttingPlaneControl()
        {
            BuildLayout();

            btnRun.Click += btnRun_Click;
            btnPrev.Click += (s, e) => ShowIteration(_index - 1);
            btnNext.Click += (s, e) => ShowIteration(_index + 1);
            btnSave.Click += btnSave_Click;
            btnRun.Enabled = false;
        }

        private void BuildLayout()
        {
            Dock = DockStyle.Fill;

            cmbOperation = new ComboBox
            {
                Location = new Point(10, 10),
                Width = 340,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbOperation.Items.AddRange(new object[]
            {
                "Cutting Plane Algorithm",
                "Duality - solve the dual and verify",
                "Shadow prices and B inverse",
                "All allowable ranges",
                "Range of a non-basic variable",
                "Change a non-basic variable",
                "Range of a basic variable",
                "Change a basic variable",
                "Range of a constraint right-hand side",
                "Change a constraint right-hand side",
                "Range of a coefficient in a non-basic column",
                "Change a coefficient in a non-basic column",
                "Add a new activity",
                "Add a new constraint"
            });
            cmbOperation.SelectedIndex = 0;

            btnRun = new Button { Text = "Run", Location = new Point(360, 9), Width = 80 };
            btnPrev = new Button { Text = "< Prev", Location = new Point(450, 9), Width = 70, Enabled = false };
            btnNext = new Button { Text = "Next >", Location = new Point(525, 9), Width = 70, Enabled = false };
            btnSave = new Button { Text = "Save results", Location = new Point(600, 9), Width = 100 };

            tableauView = new TableauView
            {
                Location = new Point(10, 45),
                Size = new Size(760, 280),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            txtNarrative = new TextBox
            {
                Location = new Point(10, 335),
                Size = new Size(760, 220),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font(FontFamily.GenericMonospace, 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.AddRange(new Control[]
            {
                cmbOperation, btnRun, btnPrev, btnNext, btnSave, tableauView, txtNarrative
            });
        }

        void IAlgorithmTab.SetModel(LPModel model)
        {
            _currentModel = model;
            btnRun.Enabled = model != null;
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            if (_currentModel == null) return;

            _iterations = new List<Tableau>();
            _labels = new List<string>();
            _writer = new OutputWriter();
            txtNarrative.Clear();

            switch (cmbOperation.SelectedIndex)
            {
                case OpCuttingPlane: RunCuttingPlane(); break;
                case OpDuality: RunDuality(); break;
                default: RunSensitivity(cmbOperation.SelectedIndex); break;
            }

            ShowIteration(0);
        }

        // -------------------------------------------------------------
        // Cutting Plane
        // -------------------------------------------------------------

        private void RunCuttingPlane()
        {
            var solver = new CuttingPlaneSolver();
            SolvedStatus result = solver.Solve(_currentModel);

            _iterations = solver.Iterations;
            _labels = solver.IterationLabels;

            _writer.WriteHeader("Cutting Plane Algorithm");
            _writer.WriteCanonicalForm(_currentModel);
            for (int i = 0; i < _iterations.Count; i++)
                _writer.WriteTableau(i, _iterations[i]);

            var narrative = new StringBuilder();
            foreach (var cut in solver.Cuts)
                narrative.AppendLine(cut.Describe());

            narrative.AppendLine($"Status: {result.Status}");
            narrative.AppendLine(result.Message);

            if (result.Status == SolverStatus.Optimal)
            {
                Solution solution = SolutionExtractor.Extract(result.FinalTableau, _currentModel);
                _writer.WriteSolution(solution);
                foreach (var kvp in solution.VariableValues)
                    narrative.AppendLine($"{kvp.Key} = {kvp.Value}");
                narrative.AppendLine($"z = {solution.ObjectiveValue}");
            }
            else
            {
                _writer.WriteError(result.Message);
            }

            Show(narrative.ToString());
        }

        // -------------------------------------------------------------
        // Duality
        // -------------------------------------------------------------

        private void RunDuality()
        {
            DualityReport report = DualityHelper.Analyse(_currentModel);
            _writer.WriteHeader("Duality");
            _writer.WriteCanonicalForm(_currentModel);
            Show(report.ToString());
        }

        // -------------------------------------------------------------
        // Sensitivity - everything else
        // -------------------------------------------------------------

        private void RunSensitivity(int operation)
        {
            Tableau canonical = CanonicalFormBuilder.Build(_currentModel);
            SolvedStatus solved = new PrimalSimplexSolver().Solve(canonical);

            if (solved.Status != SolverStatus.Optimal)
            {
                MessageBox.Show($"The model is {solved.Status}, so there is no optimal tableau to analyse.",
                    "Cannot analyse", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var analyser = new SensitivityAnalyser(solved.FinalTableau, _currentModel);
            _iterations = new List<Tableau> { solved.FinalTableau };
            _labels = new List<string> { "Optimal tableau" };

            _writer.WriteHeader(cmbOperation.SelectedItem.ToString());
            _writer.WriteCanonicalForm(_currentModel);

            int variable, constraint;
            double value;

            switch (operation)
            {
                case OpShadowPrices:
                    Show(analyser.DescribeBInverse() + Environment.NewLine + analyser.DescribeShadowPrices());
                    break;

                case OpAllRanges:
                    Show(analyser.DescribeAllRanges());
                    break;

                case OpNonBasicRange:
                    if (!AskVariable(out variable)) return;
                    Show(analyser.NonBasicVariableRange(variable).Describe());
                    break;

                case OpNonBasicChange:
                    if (!AskVariable(out variable)) return;
                    if (!AskDouble($"New objective coefficient for x{variable + 1}:", out value)) return;
                    Apply(analyser.ApplyNonBasicVariableChange(variable, value));
                    break;

                case OpBasicRange:
                    if (!AskVariable(out variable)) return;
                    Show(analyser.BasicVariableRange(variable).Describe());
                    break;

                case OpBasicChange:
                    if (!AskVariable(out variable)) return;
                    if (!AskDouble($"New objective coefficient for x{variable + 1}:", out value)) return;
                    Apply(analyser.ApplyBasicVariableChange(variable, value));
                    break;

                case OpRhsRange:
                    if (!AskConstraint(out constraint)) return;
                    Show(analyser.RhsRange(constraint).Describe());
                    break;

                case OpRhsChange:
                    if (!AskConstraint(out constraint)) return;
                    if (!AskDouble($"New right-hand side for constraint {constraint + 1}:", out value)) return;
                    Apply(analyser.ApplyRhsChange(constraint, value));
                    break;

                case OpColumnRange:
                    if (!AskVariable(out variable)) return;
                    if (!AskConstraint(out constraint)) return;
                    Show(analyser.NonBasicColumnRange(variable, constraint).Describe());
                    break;

                case OpColumnChange:
                    if (!AskVariable(out variable)) return;
                    if (!AskConstraint(out constraint)) return;
                    if (!AskDouble($"New value for a[{constraint + 1},{variable + 1}]:", out value)) return;
                    Apply(analyser.ApplyNonBasicColumnChange(variable, constraint, value));
                    break;

                case OpAddActivity:
                    RunAddActivity(analyser);
                    break;

                case OpAddConstraint:
                    RunAddConstraint(analyser);
                    break;
            }
        }

        private void RunAddActivity(SensitivityAnalyser analyser)
        {
            double c;
            if (!AskDouble("Objective coefficient for the new activity:", out c)) return;

            var a = new double[_currentModel.Constraints.Count];
            for (int i = 0; i < a.Length; i++)
                if (!AskDouble($"Coefficient in constraint {i + 1}:", out a[i])) return;

            Apply(analyser.AddActivity(a, c));
        }

        private void RunAddConstraint(SensitivityAnalyser analyser)
        {
            int n = _currentModel.ObjectiveCoefficients.Count;
            var coefficients = new double[n];
            for (int j = 0; j < n; j++)
                if (!AskDouble($"Coefficient of x{j + 1}:", out coefficients[j])) return;

            string relationText = Prompt("Relation (<=, >=, =):", "<=");
            if (relationText == null) return;

            Relation relation = relationText.Trim() == ">=" ? Relation.GreaterThanOrEqual
                              : relationText.Trim() == "=" ? Relation.Equal
                              : Relation.LessThanOrEqual;

            double rhs;
            if (!AskDouble("Right-hand side:", out rhs)) return;

            Apply(analyser.AddConstraint(coefficients, relation, rhs));
        }

        // Pushes a change's iterations into the viewer and writes them to the output file.
        private void Apply(SensitivityChange change)
        {
            if (change.Iterations.Count > 0)
            {
                _iterations = change.Iterations;
                _labels = change.IterationLabels;
            }

            for (int i = 0; i < _iterations.Count; i++)
                _writer.WriteTableau(i, _iterations[i]);

            Show(change.Explanation);
        }

        private void Show(string text)
        {
            txtNarrative.Text = text;
            _writer.WriteText(text);
        }

        // -------------------------------------------------------------
        // Iteration navigation and saving
        // -------------------------------------------------------------

        private void ShowIteration(int index)
        {
            if (_iterations == null || _iterations.Count == 0)
            {
                btnPrev.Enabled = false;
                btnNext.Enabled = false;
                return;
            }

            index = Math.Max(0, Math.Min(index, _iterations.Count - 1));
            _index = index;

            string label = _labels != null && index < _labels.Count ? _labels[index] : "Iteration";
            tableauView.DisplayTableau(_iterations[index],
                $"{label}  ({index + 1} of {_iterations.Count})");

            btnPrev.Enabled = index > 0;
            btnNext.Enabled = index < _iterations.Count - 1;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_writer == null)
            {
                MessageBox.Show("Run something first.", "Nothing to save",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Text files (*.txt)|*.txt";
                dialog.FileName = "results.txt";
                if (dialog.ShowDialog() == DialogResult.OK)
                    _writer.SaveToFile(dialog.FileName);
            }
        }

        // -------------------------------------------------------------
        // Small input prompts, since WinForms has no built-in InputBox
        // -------------------------------------------------------------

        private bool AskVariable(out int index)
        {
            index = -1;
            int n = _currentModel.ObjectiveCoefficients.Count;
            string text = Prompt($"Which variable? Enter 1 to {n}:", "1");
            if (text == null) return false;

            if (!int.TryParse(text.Trim(), out index) || index < 1 || index > n)
            {
                MessageBox.Show($"Enter a whole number between 1 and {n}.", "Invalid input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            index--;   // the analyser works in zero-based indices
            return true;
        }

        private bool AskConstraint(out int index)
        {
            index = -1;
            int m = _currentModel.Constraints.Count;
            string text = Prompt($"Which constraint? Enter 1 to {m}:", "1");
            if (text == null) return false;

            if (!int.TryParse(text.Trim(), out index) || index < 1 || index > m)
            {
                MessageBox.Show($"Enter a whole number between 1 and {m}.", "Invalid input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            index--;
            return true;
        }

        private static bool AskDouble(string caption, out double value)
        {
            value = 0;
            string text = Prompt(caption, "0");
            if (text == null) return false;

            if (!double.TryParse(text.Trim(), out value))
            {
                MessageBox.Show($"'{text}' is not a number.", "Invalid input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private static string Prompt(string caption, string defaultValue)
        {
            using (var form = new Form())
            {
                form.Text = "Input";
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.ClientSize = new Size(340, 110);
                form.MinimizeBox = false;
                form.MaximizeBox = false;

                var label = new Label { Text = caption, Location = new Point(12, 15), Width = 310 };
                var box = new TextBox { Text = defaultValue, Location = new Point(12, 40), Width = 310 };
                var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(166, 72), Width = 75 };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(247, 72), Width = 75 };

                form.Controls.AddRange(new Control[] { label, box, ok, cancel });
                form.AcceptButton = ok;
                form.CancelButton = cancel;

                return form.ShowDialog() == DialogResult.OK ? box.Text : null;
            }
        }
    }
}
