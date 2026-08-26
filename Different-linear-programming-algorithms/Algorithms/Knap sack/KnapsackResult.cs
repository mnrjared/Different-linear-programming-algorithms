using System;
using System.Collections.Generic;

namespace Different_linear_programming_algorithms.Algorithms.Knapsack
{
    /// <summary>
    /// Result returned by the Branch & Bound Knapsack solver.
    /// </summary>
    internal class KnapsackResult
    {
        public bool IsOptimal { get; set; }
        public string Message { get; set; }
        public double ObjectiveValue { get; set; }
        public double TotalWeight { get; set; }
        public double Capacity { get; set; }
        public double OptimalityGap { get; set; }

        public Dictionary<string, double> VariableValues { get; set; }
        public List<KnapsackNode> ExploredNodes { get; set; }
        public KnapsackNode BestNode { get; set; }

        public int TotalNodes { get; set; }
        public int FathomedNodes { get; set; }

        public KnapsackResult()
        {
            VariableValues = new Dictionary<string, double>();
            ExploredNodes = new List<KnapsackNode>();
        }
    }
}
