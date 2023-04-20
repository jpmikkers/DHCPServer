using GitHub.JPMikkers.DHCP;

namespace Tests;

[TestClass]
public class UnitTests
{
    [TestMethod]
    [DataRow("", new byte[0])]
    [DataRow("1", new byte[0])]
    [DataRow("0123456789ABCDEF", new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF })]
    [DataRow("0123456789ABCDEFF", new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF })]
    [DataRow("deadBEEF42", new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x42 })]
    [DataRow("DE-CA-FB-AD", new byte[] { 0xDE, 0xCA, 0xFB, 0xAD })]
    [DataRow("fE-Ed_C0^De", new byte[] { 0xFE, 0xED, 0xC0, 0xDE })]
    public void TestHexStringToBytes(string input, byte[] expectedOutput)
    {
        var result = Utils.HexStringToBytes(input);
        CollectionAssert.AreEqual(expectedOutput, result);
    }

    [TestMethod]
    [DataRow(new byte[0], "", "")]
    [DataRow(new byte[0], "-", "")]
    [DataRow(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF }, "", "0123456789ABCDEF")]
    [DataRow(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF }, "-", "01-23-45-67-89-AB-CD-EF")]
    [DataRow(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x42 }, "_", "DE_AD_BE_EF_42")]
    [DataRow(new byte[] { 0xFE, 0xED, 0xC0, 0xDE }, " ", "FE ED C0 DE" )]
    public void TestBytesToHexString(byte[] input, string separator, string expectedOutput)
    {
        var result = Utils.BytesToHexString(input, separator);
        Assert.AreEqual(expectedOutput, result);
    }
}
