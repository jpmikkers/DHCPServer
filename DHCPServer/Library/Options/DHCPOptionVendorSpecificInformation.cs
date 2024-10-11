using System.IO;

namespace GitHub.JPMikkers.DHCP;

public class DHCPOptionVendorSpecificInformation : DHCPOptionBase
{
    private byte[] _data;

    public byte[] Data
    {
        get { return _data; }
        set { _data = value; }
    }

    #region IDHCPOption Members

    public override IDHCPOption FromStream(Stream s)
    {
        DHCPOptionVendorSpecificInformation result = new DHCPOptionVendorSpecificInformation();
        result._data = new byte[s.Length];
        s.Read(result._data, 0, result._data.Length);
        return result;
    }

    public override void ToStream(Stream s)
    {
        s.Write(_data, 0, _data.Length);
    }

    #endregion

    public DHCPOptionVendorSpecificInformation()
        : base(TDHCPOption.VendorSpecificInformation)
    {
        _data = new byte[0];
    }

    public DHCPOptionVendorSpecificInformation(byte[] data)
        : base(TDHCPOption.VendorSpecificInformation)
    {
        _data = data;
    }

    public DHCPOptionVendorSpecificInformation(string data)
        : base(TDHCPOption.VendorSpecificInformation)
    {
        MemoryStream ms = new MemoryStream();
        ParseHelper.WriteString(ms, ZeroTerminatedStrings, data);
        ms.Flush();
        _data = ms.ToArray();
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{Utils.BytesToHexString(_data, " ")}])";
    }
}
