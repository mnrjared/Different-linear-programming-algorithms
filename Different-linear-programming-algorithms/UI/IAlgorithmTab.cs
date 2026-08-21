using Different_linear_programming_algorithms.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Different_linear_programming_algorithms.UI
{
    //keep model between classess
    internal interface IAlgorithmTab
    {
        void SetModel(LPModel model);
    }
}
