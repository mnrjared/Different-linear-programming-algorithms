using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Different_linear_programming_algorithms.Core;

namespace Different_linear_programming_algorithms.Algorithms.Primal_Simplex
{
    internal class RevisedTableauAdapter
    {
        public static Tableau ToDisplayTableau(RevisedIteration iteration, Tableau initial)
        {
            int m = iteration.BInverse.GetLength(0);
            int n = initial.ColCount - 1;

            var matrix = new double[m + 1, n + 1];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < m; k++)
                        sum += iteration.BInverse[i, k] * initial.Matrix[k + 1, j];
                    matrix[i + 1, j] = sum;
                }
                matrix[i + 1, n] = iteration.CurrentRHS[i];
            }
            for (int j = 0; j < n; j++)
                matrix[0, j] = iteration.ReducedCosts[j];

            return new Tableau(matrix, (int[])iteration.Basis.Clone(), initial.VarNames);
        }
    }
}
