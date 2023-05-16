using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;

namespace XHexEditor.Wpf
{
    class ZoomNotifyAdorner : Adorner
    {
        #region Construction

        public ZoomNotifyAdorner(HexEditorInternal editor, double scale)
            : base(editor)
        {
            _editor = editor;

            _ft = new FormattedText($"{(scale * 100):.}%", CultureInfo.CurrentUICulture, _editor.FlowDirection,
                new Typeface(new FontFamily("Global User Interface"), FontStyles.Normal, FontWeights.UltraBold, FontStretches.Normal), 20, Brushes.White, 96);
        }

        #endregion

        #region Fields

        private readonly HexEditorInternal _editor;
        private readonly FormattedText _ft;

        #endregion

        #region Methods

        protected override void OnRender(DrawingContext ctx)
        {
            Transform transform = (Transform)_editor.LayoutTransform.Inverse;
            ctx.PushTransform(transform);

            Point pt = new Point(_editor.ActualWidth, _editor.ActualHeight);
            pt = _editor.LayoutTransform.Transform(pt);

            const double horizPadding = 15;
            const double vertPadding = 10;

            Rect rect = new Rect(pt.X - _ft.Width - horizPadding - 5,
                                 pt.Y - _ft.Height - vertPadding - 5,
                                 _ft.Width + horizPadding,
                                 _ft.Height + vertPadding);

            ctx.DrawRectangle(HexEditorBrushes.ZoomNotificationBackground, null, rect);


            ctx.DrawText(_ft, new Point(rect.X + (horizPadding / 2), rect.Y + (vertPadding / 2)));
        }

        #endregion
    }

}
