using System;
using System.IO;

namespace GitHub.JPMikkers.DHCP;

public class DHCPOptionRenewalTimeValue : DHCPOptionBase
{
    private TimeSpan _timeSpan;

    #region IDHCPOption Members

    public TimeSpan TimeSpan
    {
        get
        {
            return _timeSpan;
        }
    }

    public override IDHCPOption FromStream(Stream s)
    {
        DHCPOptionRenewalTimeValue result = new DHCPOptionRenewalTimeValue();
        if(s.Length != 4) throw new IOException("Invalid DHCP option length");
        result._timeSpan = TimeSpan.FromSeconds(ParseHelper.ReadUInt32(s));
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteUInt32(s, (uint)_timeSpan.TotalSeconds);
    }

    #endregion

    public DHCPOptionRenewalTimeValue()
        : base(TDHCPOption.RenewalTimeValue)
    {
    }

    public DHCPOptionRenewalTimeValue(TimeSpan timeSpan)
        : base(TDHCPOption.RenewalTimeValue)
    {
        _timeSpan = timeSpan;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{_timeSpan}])";
    }
}
