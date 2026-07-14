using System;
using UIFramework.Core.Controls;
using UIFramework.Core.Skinning;

namespace UIFramework.Tests.TestSupport
{
    /// <summary>
    /// Macht die geschützten Ereignisse von SkinnedControl für Tests erreichbar.
    /// Ohne das ließe sich die Zustandsmaschine nur über echte Mauseingaben prüfen.
    /// </summary>
    public sealed class ProbeControl : SkinnedControl
    {
        protected override string ElementKey
        {
            get { return ElementKeys.Panel; }
        }

        public bool SelectedForTest { get; set; }

        protected override bool IsSelected
        {
            get { return SelectedForTest; }
        }

        public void RaiseMouseEnter()
        {
            OnMouseEnter(EventArgs.Empty);
        }

        public void RaiseMouseLeave()
        {
            OnMouseLeave(EventArgs.Empty);
        }

        public void RaiseMouseDown()
        {
            OnMouseDown(new System.Windows.Forms.MouseEventArgs(
                System.Windows.Forms.MouseButtons.Left, 1, 1, 1, 0));
        }

        public void RaiseMouseUp()
        {
            OnMouseUp(new System.Windows.Forms.MouseEventArgs(
                System.Windows.Forms.MouseButtons.Left, 1, 1, 1, 0));
        }
    }
}
