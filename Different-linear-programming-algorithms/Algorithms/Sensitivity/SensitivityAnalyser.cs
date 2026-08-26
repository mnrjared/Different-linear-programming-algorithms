using Different_linear_programming_algorithms.Algorithms.daul_simplex;
using Different_linear_programming_algorithms.Algorithms.Primal_Simplex;
using Different_linear_programming_algorithms.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Different_linear_programming_algorithms.Algorithms.Sensitivity
{
    // The verdict after changing something about an already-optimal solution.
    internal class SensitivityChange
    {
        public bool OptimumChanged { get; set; }
        public string Explanation { get; set; }
        public Tableau ResultingTableau { get; set; }
        public List<Tableau> Iterations { get; } = new List<Tableau>();
        public List<string> IterationLabels { get; } = new List<string>();
    }

    // An allowable range for one parameter, in the units the user typed it in.
    internal class SensitivityRange
    {
        public string Parameter { get; set; }
        public double Current { get; set; }
        public double Lower { get; set; }
        public double Upper { get; set; }
        public string Note { get; set; }

        public string Describe()
        {
            string lower = double.IsNegativeInfinity(Lower) ? "-infinity" : Math.Round(Lower, 3).ToString();
            string upper = double.IsPositiveInfinity(Upper) ? "+infinity" : Math.Round(Upper, 3).ToString();

            var sb = new StringBuilder();
            sb.AppendLine($"  {Parameter}");
            sb.AppendLine($"    current value  : {Math.Round(Current, 3)}");
            sb.AppendLine($"    allowable range: {lower}  to  {upper}");
            if (!string.IsNullOrEmpty(Note))
                sb.AppendLine($"    {Note}");
            return sb.ToString();
        }
    }

    // Sensitivity analysis operations that read off an optimal tableau.
    //
    // Everything rests on two things already sitting in that tableau:
    //   B_inverse       - the columns underneath the original slack variables;
    //   C_bv B_inverse  - the z row entries under those same columns (the shadow prices).
    //
    // COLUMN LOOKUP IS BY NAME. CanonicalFormBuilder does not put decision variable j in column
    // j (urs splits into x+ and x-) and does not put constraint i's slack at numVars + i
    // (equality rows get no slack). Names are the only layout-independent handle.
    //
    // CONVENTION: the tableau always maximises internally - a min model was stored as max w = -z,
    // and a "-" restricted variable had its column negated. Every calculation below stays in that
    // internal convention; the conversion back to what the user typed happens only at the edges,
    // through ObjectiveSign and DisplayObjective.
    internal class SensitivityAnalyser
    {
        private const double Tolerance = 1e-6;

        private readonly Tableau _optimal;
        private readonly LPModel _model;
        private readonly int _variableCount;
        private readonly int _constraintCount;

        public SensitivityAnalyser(Tableau optimalTableau, LPModel model)
        {
            _optimal = optimalTableau;
            _model = model;
            _variableCount = model.ObjectiveCoefficients.Count;
            _constraintCount = model.Constraints.Count;
        }

        // ===============================================================
        // Shared plumbing
        // ===============================================================

        private static int ColumnOf(Tableau t, string name)
        {
            for (int j = 0; j < t.VarNames.Length; j++)
                if (t.VarNames[j] == name) return j;
            return -1;
        }

        // The tableau column holding decision variable j.
        private int VariableColumn(int j)
        {
            int plain = ColumnOf(_optimal, $"x{j + 1}");
            return plain != -1 ? plain : ColumnOf(_optimal, $"x{j + 1}+");
        }

        private int SlackColumn(int constraintIndex)
        {
            return ColumnOf(_optimal, $"s{constraintIndex + 1}");
        }

        // The row in which a column is basic, or -1 if it is non-basic.
        private int BasicRow(int column)
        {
            for (int i = 0; i < _optimal.BasicVar.Length; i++)
                if (_optimal.BasicVar[i] == column) return i + 1;
            return -1;
        }

        public bool IsBasic(int variableIndex)
        {
            int column = VariableColumn(variableIndex);
            return column != -1 && BasicRow(column) != -1;
        }

        // Maps a user-facing objective coefficient to the internal one and back again. The two
        // directions are the same multiplication because the factor is always +1 or -1.
        private double ObjectiveSign(int j)
        {
            string restriction = j < _model.SignRestrictions.Length ? _model.SignRestrictions[j] : "+";
            double senseFactor = _model.IsMax ? 1.0 : -1.0;
            double signFactor = restriction == "-" ? -1.0 : 1.0;
            return senseFactor * signFactor;
        }

        private double InternalObjective(int j)
        {
            return ObjectiveSign(j) * _model.ObjectiveCoefficients[j];
        }

        // Converts an internal-convention z value to what the user should see.
        private double DisplayObjective(double internalZ)
        {
            return _model.IsMax ? internalZ : -internalZ;
        }

        public bool IsReadable
        {
            get
            {
                for (int i = 0; i < _constraintCount; i++)
                {
                    var c = _model.Constraints[i];
                    if (c.Relation != Relation.LessThanOrEqual) return false;
                    if (c.RHS < 0) return false;   // CanonicalFormBuilder flips it into a ">="
                    if (SlackColumn(i) == -1) return false;
                }
                return true;
            }
        }

        private string ReadabilityWarning()
        {
            return IsReadable
                ? string.Empty
                : "WARNING: this model has >= or = constraints, or a negative right-hand side that" +
                  Environment.NewLine +
                  "CanonicalFormBuilder flipped into one. Those rows carry Big-M artificials rather" +
                  Environment.NewLine +
                  "than slacks, so the values below are only reliable for the plain <= rows." +
                  Environment.NewLine;
        }

        // ===============================================================
        // 1. Shadow prices
        // ===============================================================

        // A constraint's shadow price is what one extra unit of its right-hand side is worth to
        // the objective. It sits in the z row under that constraint's slack column. A constraint
        // with slack left over is not holding the objective back, so its shadow price is zero.
        public double[] ShadowPricesRaw()
        {
            var prices = new double[_constraintCount];
            for (int i = 0; i < _constraintCount; i++)
            {
                int column = SlackColumn(i);
                prices[i] = column == -1 ? double.NaN : _optimal.Matrix[0, column];
            }
            return prices;
        }

        public string DescribeShadowPrices()
        {
            var raw = ShadowPricesRaw();
            var sb = new StringBuilder();
            sb.AppendLine("--- Shadow Prices ---");
            sb.AppendLine("(z row under the slack columns = C_bv * B_inverse)");
            sb.Append(ReadabilityWarning());

            for (int i = 0; i < raw.Length; i++)
            {
                if (double.IsNaN(raw[i]))
                {
                    sb.AppendLine($"  Constraint {i + 1}: not available (no slack column - Big-M artificial row)");
                    continue;
                }
                double display = DisplayObjective(raw[i]);
                string binding = Math.Abs(raw[i]) < Tolerance ? "not binding" : "binding";
                sb.AppendLine($"  Constraint {i + 1}: {Math.Round(display, 3),10}   ({binding})");
            }
            return sb.ToString();
        }

        public double[,] BInverse()
        {
            var b = new double[_constraintCount, _constraintCount];
            for (int r = 0; r < _constraintCount; r++)
            {
                for (int c = 0; c < _constraintCount; c++)
                {
                    int column = SlackColumn(c);
                    b[r, c] = column == -1 ? double.NaN : _optimal.Matrix[r + 1, column];
                }
            }
            return b;
        }

        public string DescribeBInverse()
        {
            var b = BInverse();
            var sb = new StringBuilder();
            sb.AppendLine("--- B Inverse ---");
            sb.Append(ReadabilityWarning());
            for (int r = 0; r < _constraintCount; r++)
            {
                sb.Append("  ");
                for (int c = 0; c < _constraintCount; c++)
                    sb.Append($"{(double.IsNaN(b[r, c]) ? "n/a" : Math.Round(b[r, c], 3).ToString()),10}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        // ===============================================================
        // 2. Range of a NON-BASIC variable's objective coefficient
        // ===============================================================

        // A non-basic variable sits at zero because it is not profitable enough to enter the
        // basis. Its z row entry d_j is how far short it falls. Raising its objective coefficient
        // by more than d_j would make it worth bringing in, which changes the solution - so the
        // coefficient can rise by exactly d_j and fall without limit.
        public SensitivityRange NonBasicVariableRange(int j)
        {
            int column = VariableColumn(j);
            if (column == -1)
                return NotAvailable($"x{j + 1} objective coefficient", "variable not found in the tableau");
            if (BasicRow(column) != -1)
                return NotAvailable($"x{j + 1} objective coefficient", "x" + (j + 1) + " is BASIC - use the basic variable range instead");

            double d = _optimal.Matrix[0, column];
            double internalCurrent = InternalObjective(j);

            // Internally the coefficient may rise by d and fall without limit.
            double internalLower = double.NegativeInfinity;
            double internalUpper = internalCurrent + d;

            return BuildObjectiveRange(j, internalLower, internalUpper,
                $"x{j + 1} objective coefficient (non-basic)",
                $"d = {Math.Round(d, 3)}; beyond this x{j + 1} enters the basis");
        }

        // Applies a new objective coefficient to a non-basic variable.
        public SensitivityChange ApplyNonBasicVariableChange(int j, double newCoefficient)
        {
            var change = new SensitivityChange();
            var sb = new StringBuilder();
            sb.AppendLine("--- Change a Non-Basic Variable's Objective Coefficient ---");

            int column = VariableColumn(j);
            if (column == -1 || BasicRow(column) != -1)
            {
                sb.AppendLine($"  x{j + 1} is not a non-basic decision variable in this tableau.");
                change.Explanation = sb.ToString();
                change.ResultingTableau = _optimal;
                return change;
            }

            double delta = ObjectiveSign(j) * (newCoefficient - _model.ObjectiveCoefficients[j]);
            var expanded = _optimal.Clone();

            // d_j = C_bv B^-1 A_j - c_j, so raising c_j by delta lowers d_j by delta.
            expanded.Matrix[0, column] -= delta;

            sb.AppendLine($"  c{j + 1}: {Math.Round(_model.ObjectiveCoefficients[j], 3)} -> {Math.Round(newCoefficient, 3)}");
            sb.AppendLine($"  new d for x{j + 1}: {Math.Round(expanded.Matrix[0, column], 3)}");

            change.Iterations.Add(expanded.Clone());
            change.IterationLabels.Add("z row updated");

            if (expanded.Matrix[0, column] >= -Tolerance)
            {
                sb.AppendLine("  Verdict: still non-negative, so the basis is unchanged and the optimal " +
                              "solution stays exactly as it was.");
                change.OptimumChanged = false;
                change.ResultingTableau = expanded;
            }
            else
            {
                sb.AppendLine("  Verdict: now negative, so x" + (j + 1) + " is worth bringing in. " +
                              "Re-optimising with the primal simplex.");
                change.OptimumChanged = true;
                change.ResultingTableau = ReoptimisePrimal(expanded, change);
            }

            change.Explanation = sb.ToString() + Environment.NewLine + Summarise(change.ResultingTableau);
            return change;
        }

        // ===============================================================
        // 3. Range of a BASIC variable's objective coefficient
        // ===============================================================

        // A basic variable's coefficient feeds into C_bv, so changing it moves EVERY non-basic
        // z row entry, not just its own. Each one gives a bound on how far the change can go
        // before that entry turns negative and the basis stops being optimal:
        //     new d_k = d_k + delta * a_rk,  which must stay >= 0
        // where r is the row the variable is basic in.
        public SensitivityRange BasicVariableRange(int j)
        {
            int column = VariableColumn(j);
            if (column == -1)
                return NotAvailable($"x{j + 1} objective coefficient", "variable not found in the tableau");

            int row = BasicRow(column);
            if (row == -1)
                return NotAvailable($"x{j + 1} objective coefficient", "x" + (j + 1) + " is NON-BASIC - use the non-basic variable range instead");

            double lowerDelta = double.NegativeInfinity;
            double upperDelta = double.PositiveInfinity;

            for (int k = 0; k < _optimal.ColCount - 1; k++)
            {
                if (k == column) continue;
                if (BasicRow(k) != -1) continue;   // only non-basic columns constrain the range

                double a = _optimal.Matrix[row, k];
                if (Math.Abs(a) < Tolerance) continue;

                double d = _optimal.Matrix[0, k];
                double bound = -d / a;

                if (a > 0) lowerDelta = Math.Max(lowerDelta, bound);
                else upperDelta = Math.Min(upperDelta, bound);
            }

            double internalCurrent = InternalObjective(j);
            return BuildObjectiveRange(j,
                double.IsNegativeInfinity(lowerDelta) ? double.NegativeInfinity : internalCurrent + lowerDelta,
                double.IsPositiveInfinity(upperDelta) ? double.PositiveInfinity : internalCurrent + upperDelta,
                $"x{j + 1} objective coefficient (basic in row {row})",
                "outside this range a different basis becomes optimal");
        }

        public SensitivityChange ApplyBasicVariableChange(int j, double newCoefficient)
        {
            var change = new SensitivityChange();
            var sb = new StringBuilder();
            sb.AppendLine("--- Change a Basic Variable's Objective Coefficient ---");

            int column = VariableColumn(j);
            int row = column == -1 ? -1 : BasicRow(column);
            if (row == -1)
            {
                sb.AppendLine($"  x{j + 1} is not a basic decision variable in this tableau.");
                change.Explanation = sb.ToString();
                change.ResultingTableau = _optimal;
                return change;
            }

            double delta = ObjectiveSign(j) * (newCoefficient - _model.ObjectiveCoefficients[j]);
            var expanded = _optimal.Clone();

            // Every entry of the z row shifts by delta times that column's entry in row r,
            // including the right-hand side, which is where the new objective value comes from.
            for (int k = 0; k < expanded.ColCount; k++)
                expanded.Matrix[0, k] += delta * expanded.Matrix[row, k];

            sb.AppendLine($"  c{j + 1}: {Math.Round(_model.ObjectiveCoefficients[j], 3)} -> {Math.Round(newCoefficient, 3)}");
            sb.AppendLine($"  z row updated by {Math.Round(delta, 3)} x row {row}");

            change.Iterations.Add(expanded.Clone());
            change.IterationLabels.Add("z row updated");

            bool stillOptimal = true;
            for (int k = 0; k < expanded.ColCount - 1; k++)
                if (expanded.Matrix[0, k] < -Tolerance) { stillOptimal = false; break; }

            if (stillOptimal)
            {
                sb.AppendLine("  Verdict: every z row entry is still non-negative, so the basis holds. " +
                              "Same variable values, new objective value.");
                change.OptimumChanged = false;
                change.ResultingTableau = expanded;
            }
            else
            {
                sb.AppendLine("  Verdict: a z row entry went negative, so the basis is no longer optimal. " +
                              "Re-optimising with the primal simplex.");
                change.OptimumChanged = true;
                change.ResultingTableau = ReoptimisePrimal(expanded, change);
            }

            change.Explanation = sb.ToString() + Environment.NewLine + Summarise(change.ResultingTableau);
            return change;
        }

        // ===============================================================
        // 4. Range of a constraint's right-hand side
        // ===============================================================

        // Changing b_i by delta moves every basic variable by delta times column i of B_inverse.
        // The basis stays feasible only while all of them stay non-negative, so each row gives a
        // bound. Inside the range the shadow price is constant, which is exactly what makes a
        // shadow price meaningful - it is only valid over this interval.
        public SensitivityRange RhsRange(int i)
        {
            int slack = SlackColumn(i);
            if (slack == -1)
                return NotAvailable($"constraint {i + 1} right-hand side", "no slack column - Big-M artificial row");

            double lowerDelta = double.NegativeInfinity;
            double upperDelta = double.PositiveInfinity;

            for (int r = 1; r < _optimal.RowCount; r++)
            {
                double u = _optimal.Matrix[r, slack];
                if (Math.Abs(u) < Tolerance) continue;

                double b = _optimal.GetRHS(r);
                double bound = -b / u;

                if (u > 0) lowerDelta = Math.Max(lowerDelta, bound);
                else upperDelta = Math.Min(upperDelta, bound);
            }

            double current = _model.Constraints[i].RHS;
            double shadow = _optimal.Matrix[0, slack];

            return new SensitivityRange
            {
                Parameter = $"constraint {i + 1} right-hand side",
                Current = current,
                Lower = double.IsNegativeInfinity(lowerDelta) ? double.NegativeInfinity : current + lowerDelta,
                Upper = double.IsPositiveInfinity(upperDelta) ? double.PositiveInfinity : current + upperDelta,
                Note = $"shadow price {Math.Round(DisplayObjective(shadow), 3)} holds across this range only"
            };
        }

        public SensitivityChange ApplyRhsChange(int i, double newRhs)
        {
            var change = new SensitivityChange();
            var sb = new StringBuilder();
            sb.AppendLine("--- Change a Constraint's Right-Hand Side ---");

            int slack = SlackColumn(i);
            if (slack == -1)
            {
                sb.AppendLine($"  Constraint {i + 1} has no slack column, so this cannot be applied directly.");
                change.Explanation = sb.ToString();
                change.ResultingTableau = _optimal;
                return change;
            }

            double delta = newRhs - _model.Constraints[i].RHS;
            var expanded = _optimal.Clone();
            int rhsIndex = expanded.ColCount - 1;

            // b* = B^-1 b, so shifting b_i by delta shifts b* by delta times column i of B^-1.
            // The same shift applied to the z row's rhs gives the new objective value, which is
            // the shadow price times delta.
            for (int r = 0; r < expanded.RowCount; r++)
                expanded.Matrix[r, rhsIndex] += delta * expanded.Matrix[r, slack];

            sb.AppendLine($"  b{i + 1}: {Math.Round(_model.Constraints[i].RHS, 3)} -> {Math.Round(newRhs, 3)}");
            sb.AppendLine($"  predicted change in z: {Math.Round(DisplayObjective(_optimal.Matrix[0, slack]) * delta, 3)}");

            change.Iterations.Add(expanded.Clone());
            change.IterationLabels.Add("Right-hand side column updated");

            bool feasible = true;
            for (int r = 1; r < expanded.RowCount; r++)
                if (expanded.GetRHS(r) < -Tolerance) { feasible = false; break; }

            if (feasible)
            {
                sb.AppendLine("  Verdict: every basic variable is still non-negative, so the basis holds. " +
                              "Same basis, new values.");
                change.OptimumChanged = false;
                change.ResultingTableau = expanded;
            }
            else
            {
                sb.AppendLine("  Verdict: a basic variable went negative, so the basis is no longer feasible. " +
                              "Re-optimising with the dual simplex.");
                change.OptimumChanged = true;
                change.ResultingTableau = ReoptimiseDual(expanded, change);
            }

            change.Explanation = sb.ToString() + Environment.NewLine + Summarise(change.ResultingTableau);
            return change;
        }

        // ===============================================================
        // 5. Range of one coefficient inside a NON-BASIC variable's column
        // ===============================================================

        // Changing a_ij for a non-basic column j only affects that column's own z row entry:
        //     new d_j = d_j + delta * y_i
        // where y_i is constraint i's shadow price. The basis holds while that stays >= 0.
        // A constraint with a zero shadow price imposes no limit at all - it has slack going
        // spare, so consuming more or less of it changes nothing.
        public SensitivityRange NonBasicColumnRange(int j, int i)
        {
            int column = VariableColumn(j);
            if (column == -1)
                return NotAvailable($"a[{i + 1},{j + 1}] in x{j + 1}'s column", "variable not found in the tableau");
            if (BasicRow(column) != -1)
                return NotAvailable($"a[{i + 1},{j + 1}] in x{j + 1}'s column", "x" + (j + 1) + " is BASIC - this operation is for non-basic columns");

            int slack = SlackColumn(i);
            if (slack == -1)
                return NotAvailable($"a[{i + 1},{j + 1}] in x{j + 1}'s column", "constraint has no slack column");

            double d = _optimal.Matrix[0, column];
            double y = _optimal.Matrix[0, slack];
            double current = _model.Constraints[i].Coefficients[j];

            if (Math.Abs(y) < Tolerance)
            {
                return new SensitivityRange
                {
                    Parameter = $"a[{i + 1},{j + 1}] in x{j + 1}'s column",
                    Current = current,
                    Lower = double.NegativeInfinity,
                    Upper = double.PositiveInfinity,
                    Note = $"constraint {i + 1} has a zero shadow price, so this coefficient does not " +
                           "affect optimality at all"
                };
            }

            double bound = current + (-d / y);
            return new SensitivityRange
            {
                Parameter = $"a[{i + 1},{j + 1}] in x{j + 1}'s column",
                Current = current,
                Lower = y > 0 ? double.NegativeInfinity : bound,
                Upper = y > 0 ? bound : double.PositiveInfinity,
                Note = $"d = {Math.Round(d, 3)}, shadow price = {Math.Round(DisplayObjective(y), 3)}"
            };
        }

        public SensitivityChange ApplyNonBasicColumnChange(int j, int i, double newValue)
        {
            var change = new SensitivityChange();
            var sb = new StringBuilder();
            sb.AppendLine("--- Change a Coefficient in a Non-Basic Variable's Column ---");

            int column = VariableColumn(j);
            int slack = SlackColumn(i);

            if (column == -1 || BasicRow(column) != -1 || slack == -1)
            {
                sb.AppendLine($"  Needs a non-basic x{j + 1} and a constraint with a slack column.");
                change.Explanation = sb.ToString();
                change.ResultingTableau = _optimal;
                return change;
            }

            double delta = newValue - _model.Constraints[i].Coefficients[j];
            var expanded = _optimal.Clone();

            // The column in the current basis is A*_j = B^-1 A_j, so a change of delta in row i
            // of the original column shifts A*_j by delta times column i of B^-1 - which is the
            // slack column. The z row entry shifts by delta times the shadow price, and the
            // slack column's own z entry IS that shadow price, so one loop does both.
            for (int r = 0; r < expanded.RowCount; r++)
                expanded.Matrix[r, column] += delta * expanded.Matrix[r, slack];

            sb.AppendLine($"  a[{i + 1},{j + 1}]: {Math.Round(_model.Constraints[i].Coefficients[j], 3)} -> {Math.Round(newValue, 3)}");
            sb.AppendLine($"  new d for x{j + 1}: {Math.Round(expanded.Matrix[0, column], 3)}");

            change.Iterations.Add(expanded.Clone());
            change.IterationLabels.Add("Column updated");

            if (expanded.Matrix[0, column] >= -Tolerance)
            {
                sb.AppendLine("  Verdict: still non-negative, so the basis is unchanged and the optimal " +
                              "solution stays exactly as it was.");
                change.OptimumChanged = false;
                change.ResultingTableau = expanded;
            }
            else
            {
                sb.AppendLine("  Verdict: now negative, so x" + (j + 1) + " has become worth bringing in. " +
                              "Re-optimising with the primal simplex.");
                change.OptimumChanged = true;
                change.ResultingTableau = ReoptimisePrimal(expanded, change);
            }

            change.Explanation = sb.ToString() + Environment.NewLine + Summarise(change.ResultingTableau);
            return change;
        }

        // ===============================================================
        // 6. Add a new activity
        // ===============================================================

        // Its column in the current basis is A* = B_inverse * a, and its reduced cost is
        // (C_bv B_inverse . a) - c. In the internal maximising convention the current solution
        // stays optimal when that reduced cost is non-negative: the activity does not earn back
        // the resources it would consume.
        public SensitivityChange AddActivity(double[] technologicalCoefficients, double objectiveCoefficient)
        {
            if (technologicalCoefficients.Length != _constraintCount)
                throw new ArgumentException("A new activity needs one coefficient per constraint.");

            var change = new SensitivityChange();
            var sb = new StringBuilder();
            sb.AppendLine("--- Add a New Activity ---");
            sb.Append(ReadabilityWarning());

            var bInverse = BInverse();

            var transformed = new double[_constraintCount];
            for (int r = 0; r < _constraintCount; r++)
            {
                double sum = 0;
                for (int c = 0; c < _constraintCount; c++)
                {
                    if (double.IsNaN(bInverse[r, c])) continue;
                    sum += bInverse[r, c] * technologicalCoefficients[c];
                }
                transformed[r] = sum;
            }

            double[] shadowRaw = ShadowPricesRaw();
            double reducedCost = 0;
            for (int i = 0; i < _constraintCount; i++)
            {
                if (double.IsNaN(shadowRaw[i])) continue;
                reducedCost += shadowRaw[i] * technologicalCoefficients[i];
            }

            double internalObjective = _model.IsMax ? objectiveCoefficient : -objectiveCoefficient;
            reducedCost -= internalObjective;

            sb.AppendLine($"  a            : ({string.Join(", ", technologicalCoefficients.Select(v => Math.Round(v, 3)))})");
            sb.AppendLine($"  c            : {Math.Round(objectiveCoefficient, 3)}");
            sb.AppendLine($"  A* = B^-1 a  : ({string.Join(", ", transformed.Select(v => Math.Round(v, 3)))})");
            sb.AppendLine($"  reduced cost : {Math.Round(reducedCost, 3)}");

            Tableau expanded = InsertActivityColumn(_optimal, transformed, reducedCost);
            change.Iterations.Add(expanded.Clone());
            change.IterationLabels.Add("New activity priced out");

            if (reducedCost >= -Tolerance)
            {
                sb.AppendLine("  Verdict      : reduced cost is non-negative, so the current solution stays " +
                              "optimal and the new activity is not worth running.");
                change.OptimumChanged = false;
                change.ResultingTableau = expanded;
            }
            else
            {
                sb.AppendLine("  Verdict      : reduced cost is negative, so the new activity improves the " +
                              "objective. Re-optimising with the primal simplex.");
                change.OptimumChanged = true;
                change.ResultingTableau = ReoptimisePrimal(expanded, change);
            }

            change.Explanation = sb.ToString();
            return change;
        }

        private Tableau InsertActivityColumn(Tableau source, double[] transformedColumn, double reducedCost)
        {
            int oldCols = source.ColCount;
            int newCol = oldCols - 1;

            var matrix = new double[source.RowCount, oldCols + 1];
            var names = new string[source.VarNames.Length + 1];

            for (int i = 0; i < source.RowCount; i++)
            {
                for (int j = 0; j < oldCols - 1; j++)
                    matrix[i, j] = source.Matrix[i, j];
                matrix[i, newCol] = 0;
                matrix[i, newCol + 1] = source.Matrix[i, oldCols - 1];
            }

            matrix[0, newCol] = reducedCost;
            for (int r = 0; r < transformedColumn.Length; r++)
                matrix[r + 1, newCol] = transformedColumn[r];

            Array.Copy(source.VarNames, names, source.VarNames.Length);
            names[newCol] = $"x{_variableCount + 1}";

            return new Tableau(matrix, (int[])source.BasicVar.Clone(), names);
        }

        // ===============================================================
        // 7. Add a new constraint
        // ===============================================================

        public SensitivityChange AddConstraint(double[] coefficients, Relation relation, double rhs)
        {
            var change = new SensitivityChange();
            var sb = new StringBuilder();
            sb.AppendLine("--- Add a New Constraint ---");

            Solution current = SolutionExtractor.Extract(_optimal, _model);
            double lhs = 0;
            for (int j = 0; j < _variableCount && j < coefficients.Length; j++)
            {
                string key = $"x{j + 1}";
                if (current.VariableValues.ContainsKey(key))
                    lhs += coefficients[j] * current.VariableValues[key];
            }

            string symbol = relation == Relation.LessThanOrEqual ? "<="
                          : relation == Relation.GreaterThanOrEqual ? ">=" : "=";
            sb.AppendLine($"  At the current optimum: {Math.Round(lhs, 3)} {symbol} {Math.Round(rhs, 3)}");

            bool alreadySatisfied =
                (relation == Relation.LessThanOrEqual && lhs <= rhs + Tolerance) ||
                (relation == Relation.GreaterThanOrEqual && lhs >= rhs - Tolerance) ||
                (relation == Relation.Equal && Math.Abs(lhs - rhs) < Tolerance);

            if (alreadySatisfied)
            {
                sb.AppendLine("  Verdict : already satisfied, so the constraint is redundant and the " +
                              "optimal solution is unchanged.");
                change.OptimumChanged = false;
                change.ResultingTableau = _optimal;
                change.Explanation = sb.ToString();
                return change;
            }

            sb.AppendLine("  Verdict : violated, so the constraint is appended and the tableau is " +
                          "re-optimised with the dual simplex.");

            var normalised = new double[_optimal.ColCount - 1];
            double normalisedRhs = rhs;
            bool flip = relation == Relation.GreaterThanOrEqual;

            for (int j = 0; j < _variableCount && j < coefficients.Length; j++)
            {
                double value = flip ? -coefficients[j] : coefficients[j];

                int plain = ColumnOf(_optimal, $"x{j + 1}");
                if (plain != -1)
                {
                    normalised[plain] = value;
                    continue;
                }

                int positive = ColumnOf(_optimal, $"x{j + 1}+");
                int negative = ColumnOf(_optimal, $"x{j + 1}-");
                if (positive != -1) normalised[positive] = value;
                if (negative != -1) normalised[negative] = -value;
            }

            if (flip) normalisedRhs = -normalisedRhs;

            Tableau expanded = _optimal.Clone();
            expanded.AppendConstraintRow(normalised, normalisedRhs);
            change.Iterations.Add(expanded.Clone());
            change.IterationLabels.Add("Constraint appended");

            // Rewrite the new row in terms of the non-basic variables.
            int newRow = expanded.RowCount - 1;
            for (int r = 1; r < newRow; r++)
            {
                int basicColumn = expanded.BasicVar[r - 1];
                double factor = expanded.Matrix[newRow, basicColumn];
                if (Math.Abs(factor) < Tolerance) continue;

                for (int j = 0; j < expanded.ColCount; j++)
                    expanded.Matrix[newRow, j] -= factor * expanded.Matrix[r, j];
            }

            change.Iterations.Add(expanded.Clone());
            change.IterationLabels.Add("New row expressed in non-basic variables");

            change.OptimumChanged = true;
            change.ResultingTableau = ReoptimiseDual(expanded, change);
            change.Explanation = sb.ToString() + Environment.NewLine + Summarise(change.ResultingTableau);
            return change;
        }

        // ===============================================================
        // Convenience: describe every range in one go, for the video
        // ===============================================================

        public string DescribeAllRanges()
        {
            var sb = new StringBuilder();
            sb.AppendLine("--- Allowable Ranges ---");
            sb.Append(ReadabilityWarning());

            sb.AppendLine();
            sb.AppendLine("Objective coefficients:");
            for (int j = 0; j < _variableCount; j++)
            {
                SensitivityRange range = IsBasic(j) ? BasicVariableRange(j) : NonBasicVariableRange(j);
                sb.Append(range.Describe());
            }

            sb.AppendLine();
            sb.AppendLine("Right-hand sides:");
            for (int i = 0; i < _constraintCount; i++)
                sb.Append(RhsRange(i).Describe());

            return sb.ToString();
        }

        // ===============================================================
        // Helpers
        // ===============================================================

        // Converts an internal-convention coefficient range back into the user's units. For a
        // min model, or a "-" restricted variable, the sign factor is negative, which swaps the
        // two ends of the interval.
        private SensitivityRange BuildObjectiveRange(int j, double internalLower, double internalUpper,
                                                     string parameter, string note)
        {
            double sign = ObjectiveSign(j);
            double a = sign * internalLower;
            double b = sign * internalUpper;

            return new SensitivityRange
            {
                Parameter = parameter,
                Current = _model.ObjectiveCoefficients[j],
                Lower = Math.Min(a, b),
                Upper = Math.Max(a, b),
                Note = note
            };
        }

        private static SensitivityRange NotAvailable(string parameter, string reason)
        {
            return new SensitivityRange
            {
                Parameter = parameter,
                Current = double.NaN,
                Lower = double.NaN,
                Upper = double.NaN,
                Note = "not available: " + reason
            };
        }

        private Tableau ReoptimisePrimal(Tableau start, SensitivityChange change)
        {
            var primal = new PrimalSimplexSolver();
            SolvedStatus result = primal.Solve(start);
            for (int i = 1; i < primal.Iterations.Count; i++)
            {
                change.Iterations.Add(primal.Iterations[i]);
                change.IterationLabels.Add($"Primal simplex iteration {i}");
            }
            return result.FinalTableau;
        }

        private Tableau ReoptimiseDual(Tableau start, SensitivityChange change)
        {
            var dual = new DualSimplexSolver();
            SolvedStatus result = dual.Solve(start);
            for (int i = 1; i < dual.Iterations.Count; i++)
            {
                change.Iterations.Add(dual.Iterations[i]);
                change.IterationLabels.Add($"Dual simplex iteration {i}");
            }
            return result.FinalTableau;
        }

        // Reads the decision variable values and objective straight off a tableau, without
        // going through SolutionExtractor - the tableau may have grown extra columns by now.
        private string Summarise(Tableau t)
        {
            if (t == null) return "  (no tableau to report)";

            var sb = new StringBuilder();
            sb.AppendLine("  Resulting solution:");
            for (int j = 0; j < _variableCount; j++)
            {
                int column = ColumnOf(t, $"x{j + 1}");
                if (column == -1) continue;

                double value = 0;
                for (int r = 0; r < t.BasicVar.Length; r++)
                    if (t.BasicVar[r] == column) value = t.GetRHS(r + 1);

                string restriction = j < _model.SignRestrictions.Length ? _model.SignRestrictions[j] : "+";
                if (restriction == "-") value = -value;

                sb.AppendLine($"    x{j + 1} = {Math.Round(value, 3)}");
            }
            sb.AppendLine($"    z  = {Math.Round(DisplayObjective(t.GetRHS(0)), 3)}");
            return sb.ToString();
        }
    }
}
