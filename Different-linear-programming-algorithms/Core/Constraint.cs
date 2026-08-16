using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Different_linear_programming_algorithms.Core
{
    public enum Relation
    {
        LessThanOrEqual,      
        GreaterThanOrEqual,   
        Equal                 
    }
    internal class Constraint
    {
        private double[] coefficients;
        private Relation relation;
        private double rhs;

        public double[] Coefficients { get => coefficients; set => coefficients = value; }
        public Relation Relation { get => relation; set => relation = value; }
        public double RHS { get => rhs; set => rhs = value; }


        public Constraint() { }

        public Constraint(double[] coefficients, Relation relation, double rhs)
        {
            Coefficients = coefficients;
            Relation = relation;
            RHS = rhs;
        }
    }
}
