using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESemulator
{
    class Mapper0 : Cartridge
    {
        public Mapper0(VirtualMachine vm, NesFile nes) : base(vm, nes) { }
    }
}
