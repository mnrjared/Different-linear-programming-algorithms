using System;

namespace Different_linear_programming_algorithms.Algorithms.Knapsack
{
    /// <summary>
    /// Represents one binary (0/1) knapsack item.
    /// </summary>
    internal class KnapsackItem
    {
        public int Index { get; set; }
        public string VariableName { get; set; }
        public double Value { get; set; }
        public double Weight { get; set; }
        public double Ratio { get; set; }

        public KnapsackItem(int index, string variableName, double value, double weight)
        {
            Index = index;
            VariableName = variableName;
            Value = value;
            Weight = weight;
            Ratio = weight > 0 ? value / weight : double.PositiveInfinity;
        }

        public KnapsackItem Clone()
        {
            return new KnapsackItem(Index, VariableName, Value, Weight);
        }
    }
}
