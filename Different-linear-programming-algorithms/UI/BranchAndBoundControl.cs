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
using Different_linear_programming_algorithms.Algorithms.BranchAndBound;

namespace Different_linear_programming_algorithms.UI
{
    public partial class BranchAndBoundControl : UserControl, IAlgorithmTab
    {
        private LPModel _currentModel;
        private BranchAndBoundSimplexSolver _solver;
        private SolvedStatus _lastResult;
        private OutputWriter _writer;
        private Dictionary<TreeNode, BnBNode> _nodeMap;

        public BranchAndBoundControl()
        {
            InitializeComponent();
            btnSolve.Enabled = false;
            btnSolve.Click += btnSolve_Click;
            btnSaveResults.Click += btnSaveResults_Click;
            treeViewNodes.AfterSelect += treeViewNodes_AfterSelect;
        }

        void IAlgorithmTab.SetModel(LPModel model)
        {
            _currentModel = model;
            btnSolve.Enabled = model != null;
        }

        private void btnSolve_Click(object sender, EventArgs e)
        {
            if (_currentModel == null) return;

            _solver = new BranchAndBoundSimplexSolver();
            _lastResult = _solver.Solve(_currentModel);

            _writer = new OutputWriter();
            _writer.WriteHeader("Branch & Bound Simplex");
            _writer.WriteCanonicalForm(_currentModel);

            BuildTree();
            WriteAllNodesToOutput();

            if (_lastResult.Status != SolverStatus.Optimal)
            {
                _writer.WriteError(_lastResult.Message);
                MessageBox.Show(_lastResult.Message,
                    _lastResult.Status == SolverStatus.Infeasible ? "Infeasible" : "Unbounded",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dgvSolution.Rows.Clear();
                return;
            }

            DisplaySolution(_lastResult.FinalTableau);
        }

        private void BuildTree()
        {
            treeViewNodes.Nodes.Clear();
            _nodeMap = new Dictionary<TreeNode, BnBNode>();
            var wrapperMap = new Dictionary<BnBNode, TreeNode>();

            foreach (var node in _solver.ExploredNodes)
            {
                string label = $"{node.BranchDescription}  [z={Math.Round(node.Bound, 3)}]  ({node.Status})";
                var treeNode = new TreeNode(label);
                _nodeMap[treeNode] = node;
                wrapperMap[node] = treeNode;

                if (node.Parent != null && wrapperMap.TryGetValue(node.Parent, out var parentTreeNode))
                    parentTreeNode.Nodes.Add(treeNode);
                else
                    treeViewNodes.Nodes.Add(treeNode);   // root
            }

            treeViewNodes.ExpandAll();
        }

        private void treeViewNodes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_nodeMap != null && _nodeMap.TryGetValue(e.Node, out var node))
                tableauView1.DisplayTableau(node.Tableau, e.Node.Text);
        }

        private void WriteAllNodesToOutput()
        {
            int iteration = 0;
            foreach (var node in _solver.ExploredNodes)
            {
                _writer.WriteHeader($"Sub-problem: {node.BranchDescription}");
                _writer.WriteTableau(iteration++, node.Tableau);
            }
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
                dialog.FileName = "bnb_results.txt";
                if (dialog.ShowDialog() == DialogResult.OK)
                    _writer.SaveToFile(dialog.FileName);
            }
        }
    }
}
