using System;
using System.Collections.Generic;
using System.Linq;

namespace Different_linear_programming_algorithms.Algorithms.Knapsack
{
    /// <summary>
    /// A node in the Branch & Bound Knapsack search tree.
    /// </summary>
    internal class KnapsackNode
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public int Depth { get; set; }

        // Items before NextIndex have already been decided.
        public int NextIndex { get; set; }

        public double CurrentWeight { get; set; }
        public double CurrentValue { get; set; }
        public double UpperBound { get; set; }

        // Decision for each item in sorted order: -1 = undecided, 0 = excluded, 1 = included.
        public int[] Decisions { get; set; }

        public string BranchDescription { get; set; }
        public string FathomReason { get; set; }
        public bool IsFathomed { get; set; }

        public KnapsackNode Clone()
        {
            return new KnapsackNode
            {
                Id = Id,
                ParentId = ParentId,
                Depth = Depth,
                NextIndex = NextIndex,
                CurrentWeight = CurrentWeight,
                CurrentValue = CurrentValue,
                UpperBound = UpperBound,
                Decisions = Decisions == null ? null : (int[])Decisions.Clone(),
                BranchDescription = BranchDescription,
                FathomReason = FathomReason,
                IsFathomed = IsFathomed
            };
        }

        public override string ToString()
        {
            return string.Format(
                "Node {0}: depth={1}, value={2:F3}, weight={3:F3}, bound={4:F3}, {5}",
                Id, Depth, CurrentValue, CurrentWeight, UpperBound, BranchDescription);
        }
    }
}
