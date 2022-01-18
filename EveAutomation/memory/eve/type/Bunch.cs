using EveAutomation.memory.python.type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.eve.type
{
    public class Bunch : PyDict
    {
        public Bunch(ulong address) : base(address)
        {
        }
    }
}
