using EveAutomation.memory.eve.type;
using EveAutomation.memory.python;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.eve
{
    public static class TypeLoader
    {
        public static void Initialize()
        {
            PyObjectTypeDeterminer.AddTypeConstructor("Bunch", address => new Bunch(address));
        }
    }
}
