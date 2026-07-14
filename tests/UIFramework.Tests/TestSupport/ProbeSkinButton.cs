using System;
using UIFramework.Controls;

namespace UIFramework.Tests.TestSupport
{
    /// <summary>
    /// Macht die geschützten Mausereignisse von SkinButton für Tests erreichbar,
    /// analog zu ProbeControl. Ohne das ließe sich Hovered/Pressed an SkinButton
    /// selbst nur über echte Mauseingaben prüfen.
    /// </summary>
    public sealed class ProbeSkinButton : SkinButton
    {
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
