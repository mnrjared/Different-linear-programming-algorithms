using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using Different_linear_programming_algorithms.Core;
using Different_linear_programming_algorithms.UI;

namespace Different_linear_programming_algorithms
{
    public partial class Form1 : Form
    {
        private LPModel _currentModel;

        public Form1()
        {
            InitializeComponent();
        }

        private void Upload_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string[] lines = File.ReadAllLines(dialog.FileName);
                        _currentModel = InputParser.Parse(lines);
                        lblFileName.Text = Path.GetFileName(dialog.FileName);

                        // push the model out to every tab, whichever algorithms exist
                        foreach (TabPage page in TabControl.TabPages)
                            foreach (Control c in page.Controls)
                                if (c is IAlgorithmTab tab)
                                    tab.SetModel(_currentModel);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not parse the file:\n{ex.Message}",
                            "Invalid model file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
