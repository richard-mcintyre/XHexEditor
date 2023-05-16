using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;

namespace XHexEditor.Wpf
{
    class CaretAdorner : Adorner
    {
        #region PInvoke

        [DllImport("user32.dll")]
        private static extern uint GetCaretBlinkTime();

        #endregion

        #region Construction

        public CaretAdorner(HexEditorInternal editor)
            : base(editor)
        {
            uint blinkTimeMS = GetCaretBlinkTime();

            _blinkTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(blinkTimeMS), DispatcherPriority.Normal, (s, a) =>
            {
                _blinkOn = !_blinkOn;
                InvalidateVisual();
            }, this.Dispatcher);
        }

        #endregion

        #region Fields

        private readonly DispatcherTimer _blinkTimer;
        private bool _blinkOn;
        private Point _location;
        private Size _size;

        #endregion

        #region Properties

        public Point Location
        {
            get => _location;
            set
            {
                _location = value;
                InvalidateVisual();
            }
        }

        public Size Size
        {
            get => _size;
            set
            {
                _size = value;
                InvalidateVisual();
            }
        }

        #endregion

        #region Methods

        public void ResetBlink()
        {
            _blinkTimer.Stop();
            _blinkOn = true;
            _blinkTimer.Start();
            InvalidateVisual();
        }

        public void StopBlink()
        {
            _blinkTimer.Stop();
            _blinkOn = true;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext ctx)
        {
            if (_blinkOn)
            {
                ctx.PushOpacity(0.5);
                ctx.DrawRectangle(Brushes.Black, null, new Rect(_location.X, _location.Y, _size.Width, _size.Height));
            }
        }

        #endregion
    }
}
