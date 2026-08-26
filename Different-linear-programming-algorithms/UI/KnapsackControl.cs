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
using Different_linear_programming_algorithms.Algorithms.Knapsack;

namespace Different_linear_programming_algorithms.UI
{
    // Person 4's tab: Branch & Bound Knapsack. Built in code like CuttingPlaneControl,
    // so it doesn't need a build-first Toolbox round trip.
    public partial class KnapsackControl : UserControl, IAlgorithmTab
    {
        private LPModel _currentModel;
        private KnapsackResult _lastResult;
        private OutputWriter _writer;
        private Dictionary<TreeNode, KnapsackNode> _nodeMap;

        private Button btnRun;
        private Button btnSaveResults;
        private Label lblSummary;
        private TreeView treeViewNodes;
        private TextBox txtNodeDetail;
        private DataGridView dgvSolution;

        public KnapsackControl()
        {
            BuildLayout();
            btnRun.Click += btnRun_Click;
            btnSaveResults.Click += btnSaveResults_Click;
            treeViewNodes.AfterSelect += treeViewNodes_AfterSelect;
            btnRun.Enabled = false;
        }

        private void BuildLayout()
        {
            Dock = DockStyle.Fill;

            btnRun = new Button { Text = "Solve Knapsack", Location = new Point(10, 10), Width = 120 };
            btnSaveResults = new Button { Text = "Save results", Location = new Point(140, 10), Width = 100 };
            lblSummary = new Label { Location = new Point(260, 15), Width = 500, AutoSize = false };

            treeViewNodes = new TreeView
            {
                Location = new Point(10, 45),
                Size = new Size(400, 400),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            txtNodeDetail = new TextBox
            {
                Location = new Point(420, 45),
                Size = new Size(350, 200),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font(FontFamily.GenericMonospace, 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            dgvSolution = new DataGridView
            {
                Location = new Point(420, 255),
                Size = new Size(350, 190),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.AddRange(new Control[] { btnRun, btnSaveResults, lblSummary, treeViewNodes, txtNodeDetail, dgvSolution });
        }

        void IAlgorithmTab.SetModel(LPModel model)
        {
            _currentModel = model;
            btnRun.Enabled = model != null;
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            if (_currentModel == null) return;

            var solver = new BranchAndBoundKnapsackSolver();

            try
            {
                _lastResult = solver.Solve(_currentModel);
            }
            catch (ArgumentException ex)
            {
                // Solve() throws rather than returning a status when the model isn't
                // shaped like a knapsack problem (not all-bin, wrong constraint count,
                // a non-<= capacity row, etc.) - this is the one solver that does that,
                // so it needs its own catch instead of a SolverStatus check.
                MessageBox.Show(ex.Message, "Not a valid knapsack model",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _writer = new OutputWriter();
            _writer.WriteHeader("Branch & Bound Knapsack");
            _writer.WriteCanonicalForm(_currentModel);

            BuildTree();
            WriteAllNodesToOutput();

            lblSummary.Text = $"{_lastResult.Message}  (nodes: {_lastResult.TotalNodes}, fathomed: {_lastResult.FathomedNodes})";

            if (!_lastResult.IsOptimal && _lastResult.BestNode == null)
            {
                MessageBox.Show(_lastResult.Message, "No feasible solution",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dgvSolution.Rows.Clear();
                return;
            }

            DisplaySolution();
        }

        private void BuildTree()
        {
            treeViewNodes.Nodes.Clear();
            _nodeMap = new Dictionary<TreeNode, KnapsackNode>();
            var byId = new Dictionary<int, TreeNode>();

            foreach (var node in _lastResult.ExploredNodes)
            {
                string label = $"{node.BranchDescription}  [value={Math.Round(node.CurrentValue, 3)}, " +
                               $"weight={Math.Round(node.CurrentWeight, 3)}, bound={Math.Round(node.UpperBound, 3)}]";
                var treeNode = new TreeNode(label);
                _nodeMap[treeNode] = node;
                byId[node.Id] = treeNode;

                if (node.ParentId >= 0 && byId.TryGetValue(node.ParentId, out var parentTreeNode))
                    parentTreeNode.Nodes.Add(treeNode);
                else
                    treeViewNodes.Nodes.Add(treeNode);   // root
            }

            treeViewNodes.ExpandAll();
        }

        private void treeViewNodes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_nodeMap == null || !_nodeMap.TryGetValue(e.Node, out var node)) return;

            var lines = new List<string>
            {
                $"Depth: {node.Depth}",
                $"Value so far: {Math.Round(node.CurrentValue, 3)}",
                $"Weight so far: {Math.Round(node.CurrentWeight, 3)}",
                $"Upper bound: {Math.Round(node.UpperBound, 3)}",
                node.IsFathomed ? $"Fathomed: {node.FathomReason}" : "Not fathomed"
            };
            txtNodeDetail.Text = string.Join(Environment.NewLine, lines);
        }

        private void WriteAllNodesToOutput()
        {
            foreach (var node in _lastResult.ExploredNodes)
                _writer.WriteHeader(node.ToString());
        }

        private void DisplaySolution()
        {
            dgvSolution.Columns.Clear();
            dgvSolution.Rows.Clear();
            dgvSolution.Columns.Add("var", "Variable");
            dgvSolution.Columns.Add("val", "Value");

            foreach (var kvp in _lastResult.VariableValues)
                dgvSolution.Rows.Add(kvp.Key, kvp.Value);

            dgvSolution.Rows.Add("Total weight", _lastResult.TotalWeight);
            dgvSolution.Rows.Add("Capacity", _lastResult.Capacity);

            var solution = new Solution
            {
                VariableValues = _lastResult.VariableValues,
                ObjectiveValue = _lastResult.ObjectiveValue
            };
            _writer.WriteSolution(solution);
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
                dialog.FileName = "knapsack_results.txt";
                if (dialog.ShowDialog() == DialogResult.OK)
                    _writer.SaveToFile(dialog.FileName);
            }
        }
    }
}
