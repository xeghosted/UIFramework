using System.Drawing;
using System.Windows.Forms;
using UIFramework.Core.Skinning;

namespace UIFramework.Tests.TestSupport
{
    /// <summary>
    /// Ein Skin mit einer einzigen, frei wählbaren Farbe — damit Pixelprüfungen
    /// eine unverwechselbare Farbe erwarten können, statt sich an die
    /// mitgelieferten Skins zu binden.
    /// </summary>
    public sealed class StubSkin : SkinBase
    {
        private readonly string _name;

        public StubSkin(Color background, string name = "Stub")
        {
            _name = name;

            var appearance = new ElementAppearance
            {
                Background = background,
                BackgroundGradientEnd = null,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Corners = CornerRadius.None,
                ForeColor = Color.FromArgb(255, 255, 255, 255),
                Font = new FontSpec("Segoe UI", 9f),
                Padding = new Padding(4)
            };

            foreach (var element in new[] { ElementKeys.Button, ElementKeys.Panel, ElementKeys.Label, ElementKeys.Focus })
            {
                Define(element, ElementState.Normal, appearance);
            }
        }

        public override string Name
        {
            get { return _name; }
        }
    }
}
