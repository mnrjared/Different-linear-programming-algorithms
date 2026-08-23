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
        private const double Epsilon = 1e-16;

        public static bool IsInteger(double value) =>
            Math.Abs(value - Math.Round(value)) < Epsilon;

        public static bool IsPositive(double value) => value > Epsilon;
        public static bool IsZero(double value) => Math.Abs(value) < Epsilon;

        public static bool IsNegative(double value) => value < -Epsilon;
    }
}
