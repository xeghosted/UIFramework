using UIFramework.Controls;

namespace UIFramework.Tests.TestSupport
{
    /// <summary>
    /// Erlaubt, Focused deterministisch zu simulieren, ohne dem Testprozess
    /// echten Betriebssystem-Fokus abringen zu müssen (SetFocus/Aktivierung sind
    /// in einer automatisierten Umgebung nicht zuverlässig steuerbar). Control.Focused
    /// ist virtual — genau dafür gedacht (ComboBox & Co. überschreiben es ebenso,
    /// um zusammengesetzte Controls abzubilden).
    /// </summary>
    public sealed class ProbeFocusableSkinButton : SkinButton
    {
        public bool FocusedOverride { get; set; }

        public override bool Focused
        {
            get { return FocusedOverride; }
        }
    }
}
