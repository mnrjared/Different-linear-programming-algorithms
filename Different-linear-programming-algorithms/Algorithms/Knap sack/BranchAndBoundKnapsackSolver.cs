using System;
using System.Collections.Generic;
using System.Linq;
using Different_linear_programming_algorithms.Core;

namespace Different_linear_programming_algorithms.Algorithms.Knapsack
{
    /// <summary>
    /// Branch & Bound solver for a binary (0/1) maximisation knapsack problem.
    ///
    /// The implementation is deliberately independent of Tableau/Pivot. It uses
    /// a fractional-knapsack upper bound and a stack for depth-first backtracking.
    /// </summary>
    internal class BranchAndBoundKnapsackSolver
    {
        private const double Epsilon = 1e-9;
        private const int MaxNodes = 100000;
        private int _nextNodeId;

        public List<KnapsackNode> ExploredNodes { get; private set; }
        public int TotalNodes { get; private set; }
        public int FathomedNodes { get; private set; }
        public KnapsackNode BestNode { get; private set; }

        public BranchAndBoundKnapsackSolver()
        {
            ExploredNodes = new List<KnapsackNode>();
        }

        /// <summary>
        /// Solves the binary knapsack model contained in an LPModel.
        /// The model must be a maximisation problem with one <= capacity constraint
        /// and all decision variables marked as bin.
        /// </summary>
        public KnapsackResult Solve(LPModel model)
        {
            ValidateModel(model);

            int itemCount = model.ObjectiveCoefficients.Count;
            Constraint capacityConstraint = model.Constraints[0];

            var items = new List<KnapsackItem>();
            for (int i = 0; i < itemCount; i++)
            {
                double value = model.ObjectiveCoefficients[i];
                double weight = capacityConstraint.Coefficients[i];
                items.Add(new KnapsackItem(i, "x" + (i + 1), value, weight));
            }

            // The fractional upper bound is strongest when the items are ordered
            // by decreasing value/weight ratio.
            items = items
                .OrderByDescending(item => item.Ratio)
                .ThenBy(item => item.Index)
                .ToList();

            double capacity = capacityConstraint.RHS;
            ResetState();

            var root = CreateRootNode(items.Count);
            root.UpperBound = CalculateUpperBound(root, items, capacity);

            var stack = new Stack<KnapsackNode>();
            stack.Push(root);

            double bestValue = double.NegativeInfinity;
            double bestWeight = 0;
            int[] bestDecisions = null;
            KnapsackNode bestNode = null;

            while (stack.Count > 0 && TotalNodes < MaxNodes)
            {
                KnapsackNode node = stack.Pop();
                TotalNodes++;
                ExploredNodes.Add(node);

                // Capacity violation means the node can never become feasible.
                if (node.CurrentWeight > capacity + Epsilon)
                {
                    Fathom(node, "Infeasible: total weight exceeds capacity.");
                    continue;
                }

                // An upper bound equal to or below the incumbent cannot improve it.
                if (bestDecisions != null && node.UpperBound <= bestValue + Epsilon)
                {
                    Fathom(node, "Fathomed: upper bound cannot improve the incumbent.");
                    continue;
                }

                // Every item has been decided, so this is an integer-feasible candidate.
                if (node.NextIndex >= items.Count)
                {
                    if (node.CurrentValue > bestValue + Epsilon)
                    {
                        bestValue = node.CurrentValue;
                        bestWeight = node.CurrentWeight;
                        bestDecisions = (int[])node.Decisions.Clone();
                        bestNode = node;
                        node.FathomReason = "Integer-feasible candidate; incumbent updated.";
                    }
                    else
                    {
                        node.FathomReason = "Integer-feasible candidate; incumbent not improved.";
                    }

                    node.IsFathomed = true;
                    FathomedNodes++;
                    continue;
                }

                int branchIndex = node.NextIndex;

                // Create both branches. We push exclude first and include second so
                // the include branch is explored first by the LIFO stack.
                KnapsackNode excludeChild = CreateChild(
                    node,
                    items,
                    branchIndex,
                    include: false,
                    capacity: capacity);

                KnapsackNode includeChild = CreateChild(
                    node,
                    items,
                    branchIndex,
                    include: true,
                    capacity: capacity);

                stack.Push(excludeChild);
                stack.Push(includeChild);
            }

            bool stoppedByLimit = stack.Count > 0;

            if (bestDecisions == null)
            {
                return BuildResult(
                    items,
                    capacity,
                    false,
                    "No feasible binary solution was found.",
                    bestValue,
                    bestWeight,
                    null,
                    stoppedByLimit);
            }

            var result = BuildResult(
                items,
                capacity,
                !stoppedByLimit,
                stoppedByLimit
                    ? "A feasible incumbent was found, but the search node limit was reached."
                    : "Optimal binary knapsack solution found.",
                bestValue,
                bestWeight,
                bestDecisions,
                stoppedByLimit);

            BestNode = bestNode;
            result.BestNode = bestNode;
            return result;
        }

        /// <summary>
        /// Convenience overload for tests or callers that already have item data.
        /// </summary>
        public KnapsackResult Solve(IList<KnapsackItem> inputItems, double capacity)
        {
            if (inputItems == null || inputItems.Count == 0)
                throw new ArgumentException("At least one knapsack item is required.");

            if (capacity < -Epsilon)
                throw new ArgumentException("Knapsack capacity cannot be negative.");

            var items = inputItems.Select(item => item.Clone()).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Weight < -Epsilon)
                    throw new ArgumentException("Knapsack item weights cannot be negative.");
                if (items[i].Value < -Epsilon)
                    throw new ArgumentException("Knapsack item values cannot be negative for this maximisation solver.");
            }

            items = items
                .OrderByDescending(item => item.Ratio)
                .ThenBy(item => item.Index)
                .ToList();

            ResetState();

            var root = CreateRootNode(items.Count);
            root.UpperBound = CalculateUpperBound(root, items, capacity);

            var stack = new Stack<KnapsackNode>();
            stack.Push(root);

            double bestValue = double.NegativeInfinity;
            double bestWeight = 0;
            int[] bestDecisions = null;
            KnapsackNode bestNode = null;

            while (stack.Count > 0 && TotalNodes < MaxNodes)
            {
                KnapsackNode node = stack.Pop();
                TotalNodes++;
                ExploredNodes.Add(node);

                if (node.CurrentWeight > capacity + Epsilon)
                {
                    Fathom(node, "Infeasible: total weight exceeds capacity.");
                    continue;
                }

                if (bestDecisions != null && node.UpperBound <= bestValue + Epsilon)
                {
                    Fathom(node, "Fathomed: upper bound cannot improve the incumbent.");
                    continue;
                }

                if (node.NextIndex >= items.Count)
                {
                    if (node.CurrentValue > bestValue + Epsilon)
                    {
                        bestValue = node.CurrentValue;
                        bestWeight = node.CurrentWeight;
                        bestDecisions = (int[])node.Decisions.Clone();
                        bestNode = node;
                    }

                    Fathom(node, "Integer-feasible candidate.");
                    continue;
                }

                int branchIndex = node.NextIndex;
                KnapsackNode excludeChild = CreateChild(node, items, branchIndex, false, capacity);
                KnapsackNode includeChild = CreateChild(node, items, branchIndex, true, capacity);

                stack.Push(excludeChild);
                stack.Push(includeChild);
            }

            bool stoppedByLimit = stack.Count > 0;
            BestNode = bestNode;

            var result = BuildResult(
                items,
                capacity,
                bestDecisions != null && !stoppedByLimit,
                bestDecisions == null
                    ? "No feasible binary solution was found."
                    : stoppedByLimit
                        ? "A feasible incumbent was found, but the search node limit was reached."
                        : "Optimal binary knapsack solution found.",
                bestValue,
                bestWeight,
                bestDecisions,
                stoppedByLimit);

            result.BestNode = bestNode;
            return result;
        }

        private KnapsackNode CreateRootNode(int itemCount)
        {
            return new KnapsackNode
            {
                Id = _nextNodeId++,
                ParentId = -1,
                Depth = 0,
                NextIndex = 0,
                CurrentWeight = 0,
                CurrentValue = 0,
                Decisions = Enumerable.Repeat(-1, itemCount).ToArray(),
                BranchDescription = "Root node",
                IsFathomed = false
            };
        }

        private KnapsackNode CreateChild(
            KnapsackNode parent,
            IList<KnapsackItem> items,
            int itemIndex,
            bool include,
            double capacity)
        {
            var child = new KnapsackNode
            {
                Id = _nextNodeId++,
                ParentId = parent.Id,
                Depth = parent.Depth + 1,
                NextIndex = itemIndex + 1,
                CurrentWeight = parent.CurrentWeight,
                CurrentValue = parent.CurrentValue,
                Decisions = (int[])parent.Decisions.Clone(),
                BranchDescription = include
                    ? "Include " + items[itemIndex].VariableName
                    : "Exclude " + items[itemIndex].VariableName,
                IsFathomed = false
            };

            child.Decisions[itemIndex] = include ? 1 : 0;

            if (include)
            {
                child.CurrentWeight += items[itemIndex].Weight;
                child.CurrentValue += items[itemIndex].Value;
            }

            child.UpperBound = CalculateUpperBound(child, items, capacity);
            return child;
        }

        /// <summary>
        /// Computes the fractional-knapsack upper bound from the current node.
        /// Remaining capacity may be filled fractionally, so the bound can never
        /// underestimate the best integer solution below this node.
        /// </summary>
        private double CalculateUpperBound(KnapsackNode node, IList<KnapsackItem> items, double capacity)
        {
            if (node.CurrentWeight > capacity + Epsilon)
                return double.NegativeInfinity;

            double remainingCapacity = capacity - node.CurrentWeight;
            double bound = node.CurrentValue;

            for (int i = node.NextIndex; i < items.Count; i++)
            {
                KnapsackItem item = items[i];

                if (item.Weight <= Epsilon)
                {
                    // A non-negative zero-weight item should always be included.
                    if (item.Value > Epsilon)
                        bound += item.Value;
                    continue;
                }

                if (item.Weight <= remainingCapacity + Epsilon)
                {
                    remainingCapacity -= item.Weight;
                    bound += item.Value;
                }
                else
                {
                    // Take only the fraction that fits. This is the upper bound.
                    bound += item.Ratio * Math.Max(0, remainingCapacity);
                    break;
                }
            }

            return bound;
        }

        private KnapsackResult BuildResult(
            IList<KnapsackItem> items,
            double capacity,
            bool optimal,
            string message,
            double bestValue,
            double bestWeight,
            int[] bestDecisions,
            bool stoppedByLimit)
        {
            var result = new KnapsackResult
            {
                IsOptimal = optimal,
                Message = message,
                ObjectiveValue = bestDecisions == null ? 0 : bestValue,
                TotalWeight = bestDecisions == null ? 0 : bestWeight,
                Capacity = capacity,
                TotalNodes = TotalNodes,
                FathomedNodes = FathomedNodes,
                ExploredNodes = new List<KnapsackNode>(ExploredNodes),
                BestNode = BestNode,
                OptimalityGap = 0
            };

            foreach (KnapsackItem item in items)
            {
                int sortedIndex = items.IndexOf(item);
                double selected = bestDecisions != null && bestDecisions[sortedIndex] == 1 ? 1 : 0;
                result.VariableValues[item.VariableName] = selected;
            }

            return result;
        }

        private void Fathom(KnapsackNode node, string reason)
        {
            node.IsFathomed = true;
            node.FathomReason = reason;
            FathomedNodes++;
        }

        private void ResetState()
        {
            ExploredNodes.Clear();
            TotalNodes = 0;
            FathomedNodes = 0;
            BestNode = null;
            _nextNodeId = 0;
        }

        private void ValidateModel(LPModel model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (!model.IsMax)
                throw new ArgumentException("Branch & Bound Knapsack requires a maximisation model.");

            if (model.ObjectiveCoefficients == null || model.ObjectiveCoefficients.Count == 0)
                throw new ArgumentException("The knapsack model must contain at least one decision variable.");

            if (model.Constraints == null || model.Constraints.Count != 1)
                throw new ArgumentException("The Branch & Bound Knapsack solver expects exactly one capacity constraint.");

            Constraint constraint = model.Constraints[0];
            if (constraint.Relation != Relation.LessThanOrEqual)
                throw new ArgumentException("The knapsack capacity constraint must use <=.");

            if (constraint.Coefficients == null || constraint.Coefficients.Length != model.ObjectiveCoefficients.Count)
                throw new ArgumentException("The capacity constraint must have one weight coefficient for every decision variable.");

            if (constraint.RHS < -Epsilon)
                throw new ArgumentException("Knapsack capacity cannot be negative.");

            if (model.SignRestrictions == null || model.SignRestrictions.Length != model.ObjectiveCoefficients.Count)
                throw new ArgumentException("Every knapsack decision variable must have a sign restriction.");

            for (int i = 0; i < model.ObjectiveCoefficients.Count; i++)
            {
                if (model.SignRestrictions[i] != "bin")
                    throw new ArgumentException("Variable x" + (i + 1) + " must have the bin sign restriction.");

                if (model.ObjectiveCoefficients[i] < -Epsilon)
                    throw new ArgumentException("Knapsack objective coefficients must be non-negative.");

                if (constraint.Coefficients[i] < -Epsilon)
                    throw new ArgumentException("Knapsack weights must be non-negative.");
            }
        }
    }
}
