using UIFramework.Controls;
using Xunit;

namespace UIFramework.Tests.Menus
{
    public class MnemonicsTests
    {
        [Theory]
        [InlineData("&Datei", 'D')]
        [InlineData("D&atei", 'A')]      // großgeschrieben zurückgegeben
        [InlineData("Datei", '\0')]
        [InlineData("", '\0')]
        [InlineData(null, '\0')]
        [InlineData("A && B", '\0')]     // && ist ein Literal, kein Mnemonic
        [InlineData("A && &B", 'B')]     // Literal davor, Mnemonic dahinter
        [InlineData("Datei&", '\0')]     // & am Ende markiert nichts
        public void FromText_extracts_the_mnemonic(string text, char expected)
        {
            Assert.Equal(expected, Mnemonics.FromText(text));
        }
    }
}
