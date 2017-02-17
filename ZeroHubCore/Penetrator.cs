using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroHub
{
    public interface Penetrator
    {
        bool Penetrate();
        string DestIP { get; set; }
        int DestPort { get; set; }
        string SourceIP { get; set; }
        int SourcePort { get; set; }
    }
}
