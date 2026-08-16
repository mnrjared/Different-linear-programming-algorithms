using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Different_linear_programming_algorithms.Core
{
    public enum SolverStatus 
    {
        Optimal, 
        Infeasible, 
        Unbounded 
    }
    internal class SolvedStatus
    {
        private SolverStatus status;
        private Tableau finalTableau;
        private string message;

        

        public SolverStatus Status { get => status; set => status = value; }
        public string Message { get => message; set => message = value; }
        public Tableau FinalTableau { get => finalTableau; set => finalTableau = value; }
        public SolvedStatus() { }
        public SolvedStatus(SolverStatus status, string message, Tableau finalTableau)
        {
            Status = status;
            Message = message;
            FinalTableau = finalTableau;
        }
    }
}
