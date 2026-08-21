using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Different_linear_programming_algorithms.Core;
using Different_linear_programming_algorithms.Algorithms.Primal_Simplex;

namespace Different_linear_programming_algorithms.UI
{
    public partial class PrimalSimplexControl : UserControl, IAlgorithmTab
    {
        private LPModel _currentModel;
        private List<Tableau> _displayIterations;
        private int _currentIterationIndex;
        private SolvedStatus _lastResult;
        private OutputWriter _writer;

        public PrimalSimplexControl()
        {
            InitializeComponent();
            cmbAlgorithm.Items.AddRange(new object[] { "Primal Simplex", "Revised Primal Simplex" });
            cmbAlgorithm.SelectedIndex = 0;
            btnSolve.Enabled = false;
            btnPrev.Click += (s, e) => ShowIteration(_currentIterationIndex - 1);
            btnNext.Click += (s, e) => ShowIteration(_currentIterationIndex + 1);
            btnSolve.Click += btnSolve_Click;
            btnSaveResults.Click += btnSaveResults_Click;
        }

        void IAlgorithmTab.SetModel(LPModel model)
        {
            _currentModel = model;
            btnSolve.Enabled = model != null;
        }

        private void btnSolve_Click(object sender, EventArgs e)
        {
            if (_currentModel == null) return;

            Tableau initial = CanonicalFormBuilder.Build(_currentModel);
            _displayIterations = new List<Tableau>();

            if (cmbAlgorithm.SelectedIndex == 0)
            {
                var solver = new PrimalSimplexSolver();
                _lastResult = solver.Solve(initial);
                _displayIterations = solver.Iterations;
            }
            else
            {
                var solver = new RevisedPrimalSimplexSolver();
                _lastResult = solver.Solve(_currentModel);
                foreach (var iter in solver.Iterations)
                    _displayIterations.Add(RevisedTableauAdapter.ToDisplayTableau(iter, initial));
            }

            _writer = new OutputWriter();
            _writer.WriteHeader(cmbAlgorithm.SelectedItem.ToString());
            _writer.WriteCanonicalForm(_currentModel);
            for (int i = 0; i < _displayIterations.Count; i++)
                _writer.WriteTableau(i, _displayIterations[i]);

            if (_lastResult.Status != SolverStatus.Optimal)
            {
                _writer.WriteError(_lastResult.Message);
                MessageBox.Show(_lastResult.Message,
                    _lastResult.Status == SolverStatus.Infeasible ? "Infeasible" : "Unbounded",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dgvSolution.Rows.Clear();
                return;
            }

            ShowIteration(0);
            DisplaySolution(_lastResult.FinalTableau);
        }

        private void ShowIteration(int index)
        {
            if (_displayIterations == null || _displayIterations.Count == 0) return;
            index = Math.Max(0, Math.Min(index, _displayIterations.Count - 1));
            _currentIterationIndex = index;

            tableauView1.DisplayTableau(_displayIterations[index], $"Iteration {index + 1} of {_displayIterations.Count}");
            btnPrev.Enabled = index > 0;
            btnNext.Enabled = index < _displayIterations.Count - 1;
        }

        private void DisplaySolution(Tableau finalTableau)
        {
            var solution = SolutionExtractor.Extract(finalTableau, _currentModel);
            _writer.WriteSolution(solution);

            dgvSolution.Columns.Clear();
            dgvSolution.Rows.Clear();
            dgvSolution.Columns.Add("var", "Variable");
            dgvSolution.Columns.Add("val", "Value");

            foreach (var kvp in solution.VariableValues)
                dgvSolution.Rows.Add(kvp.Key, kvp.Value);
            dgvSolution.Rows.Add("z", solution.ObjectiveValue);
        }

        private void btnSaveResults_Click(object sender, EventArgs e)
        {
            if (_writer == null)
            {
                MessageBox.Show("Solve a model first.", "Nothing to save",
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
    }
}
