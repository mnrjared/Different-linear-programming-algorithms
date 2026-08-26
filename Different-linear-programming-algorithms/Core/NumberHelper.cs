using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Different_linear_programming_algorithms.Core
{
    //to check wether a value is an integer,zero,negative or positive
    internal class NumberHelper
    {
        // WAS 1e-16 IN THE LAST COMMIT. That is smaller than double.Epsilon's practical
        // resolution: machine epsilon for a double is about 2.22e-16, so after a handful of
        // pivots a value that should be exactly 0 typically sits somewhere around 1e-15 and a
        // value that should be exactly 2 sits at 2.0000000000000004.
        //
        // With the tolerance at 1e-16 that floating point noise is treated as real, which
        // breaks four things at once:
        //   - PrimalSimplexSolver.IsOptimal sees a z row entry of -3e-16 as still negative and
        //     keeps pivoting. That loop has no iteration cap, so the app hangs.
        //   - PrimalSimplexSolver.HasNonZeroArtificial sees a residual artificial of 1e-15 as
        //     non-zero and reports a feasible model as Infeasible.
        //   - BranchAndBound.IsIntegerFeasible never accepts a solution as integer, so no
        //     incumbent is ever recorded and it reports "No integer-feasible solution found".
        //   - CuttingPlaneSolver never stops cutting, and runs to its cut limit.
        //
        // 1e-6 is the value it had before and is the right order of magnitude for a tableau
        // built from integer-ish coefficients. Do not lower it again.
        private const double Epsilon = 1e-6;

        public static bool IsInteger(double value) =>
            Math.Abs(value - Math.Round(value)) < Epsilon;

        public static bool IsPositive(double value) => value > Epsilon;
        public static bool IsZero(double value) => Math.Abs(value) < Epsilon;

        public static bool IsNegative(double value) => value < -Epsilon;
    }
}
