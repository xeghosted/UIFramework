using System;
using System.Drawing;
using UIFramework.Controls;

namespace UIFramework.Tests.TestSupport
{
    /// <summary>
    /// Macht die geschützten Mausereignisse von SkinScrollBar für Tests erreichbar,
    /// analog zu ProbeControl/ProbeSkinButton. Anders als dort braucht die Leiste
    /// eine echte Koordinate — Daumengriff und Rinnenklick unterscheiden sich nur
    /// durch die Position —, darum nehmen die Raise-Methoden hier einen Point.
    /// </summary>
    public sealed class ProbeSkinScrollBar : SkinScrollBar
    {
        public void RaiseMouseDown(Point location)
        {
            OnMouseDown(new System.Windows.Forms.MouseEventArgs(
                System.Windows.Forms.MouseButtons.Left, 1, location.X, location.Y, 0));
        }

        public void RaiseMouseMove(Point location)
        {
            OnMouseMove(new System.Windows.Forms.MouseEventArgs(
                System.Windows.Forms.MouseButtons.None, 0, location.X, location.Y, 0));
        }

        public void RaiseMouseUp(Point location)
        {
            OnMouseUp(new System.Windows.Forms.MouseEventArgs(
                System.Windows.Forms.MouseButtons.Left, 1, location.X, location.Y, 0));
        }

        public void RaiseMouseLeave()
        {
            OnMouseLeave(EventArgs.Empty);
        }
    }
}
