using System;

namespace GitHub.JPMikkers.DHCP;

[Serializable]
public class UDPSocketException : Exception
{
    public required bool IsFatal { get; init; }

    public UDPSocketException() 
    { 
    }

    public UDPSocketException(string message) : base(message)
    { 
    }

    public UDPSocketException(string message, Exception inner) : base(message, inner) 
    { 
    }
}