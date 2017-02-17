using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroHub
{
    public enum ENatType
    {
        FullCone,
        RestrictedPort,
        RestrictedCone,
        Symmetric
    }

    public enum ETraversal
    {
        Relay,
        UPnP,
        PMP,  
        UDPHolePunching,
        TCPHolePunching,
        ICMPHolePunching
    }

    public enum EHolePunching
    {
        Stun,
        PortPredict
    }
}
