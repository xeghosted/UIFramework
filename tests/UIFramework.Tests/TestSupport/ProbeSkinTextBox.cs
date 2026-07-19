using System;
using UIFramework.Controls;

namespace UIFramework.Tests.TestSupport
{
    /// <summary>
    /// Erlaubt, den Fokuszustand von SkinTextBox deterministisch zu simulieren,
    /// analog zu ProbeFocusableSkinButton — SkinTextBox spiegelt IsSelected auf
    /// das Fokus-Flag des eingebetteten nativen TextBox, das sich in einem
    /// automatisierten Testhost nicht zuverlässig echten Fokus geben lässt.
    /// </summary>
    public sealed class ProbeSkinTextBox : SkinTextBox
    {
        public bool FocusedOverride { get; set; }

        protected override bool IsSelected
        {
            get { return FocusedOverride; }
        }

        public void RaiseMouseEnter()
        {
            OnMouseEnter(EventArgs.Empty);
        }

        public void RaiseMouseLeave()
        {
            OnMouseLeave(EventArgs.Empty);
        }
    }
}
