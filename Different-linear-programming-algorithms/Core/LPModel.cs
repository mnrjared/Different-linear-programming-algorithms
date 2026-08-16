using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Different_linear_programming_algorithms.Core
{
    internal class LPModel
    {
        private bool isMax;
        private List<double> objectiveCoefficients;
        private List<Constraint> constraints = new List<Constraint>();
        private string[] signRestrictions;
        public bool IsMax { get => isMax; set => isMax = value; }
        public List<double> ObjectiveCoefficients { get => objectiveCoefficients; set => objectiveCoefficients = value; }
        public List<Constraint> Constraints { get => constraints; set => constraints = value; }
        public string[] SignRestrictions { get => signRestrictions; set => signRestrictions = value; }

        public LPModel() { }
        public LPModel(bool isMax, List<double> objectiveCoefficients, List<Constraint> constraints, string[] signRestrictions)
        {
            IsMax = isMax;
            ObjectiveCoefficients = objectiveCoefficients;
            Constraints = constraints;
            SignRestrictions = signRestrictions;
        }
    }
}
