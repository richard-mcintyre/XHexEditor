using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace XHexEditor.Wpf
{
    internal class TextMetrics
    {
        #region Construction

        public TextMetrics(Typeface typeface, int fontSize, FlowDirection flowDirection)
        {
            _emFontSize = fontSize * (96 / 72.0);

            _ftHexDigits = new FormattedText("00", CultureInfo.CurrentUICulture, flowDirection, typeface, _emFontSize, null, 96);
            _ftSpace = new FormattedText(" ", CultureInfo.CurrentUICulture, flowDirection, typeface, _emFontSize, null, 96);
            _ftAscii = new FormattedText(".", CultureInfo.CurrentUICulture, flowDirection, typeface, _emFontSize, null, 96);
            _ftDigit = new FormattedText("0", CultureInfo.CurrentUICulture, flowDirection, typeface, _emFontSize, null, 96);
        }

        #endregion

        #region Fields

        private readonly double _emFontSize;
        private readonly FormattedText _ftHexDigits;
        private readonly FormattedText _ftSpace;
        private readonly FormattedText _ftAscii;
        private readonly FormattedText _ftDigit;

        #endregion

        #region Properties

        /// <summary>
        /// EM size of the font
        /// </summary>
        public double EMFontSize => _emFontSize;

        /// <summary>
        /// Gets the height of a single line
        /// </summary>
        public double LineHeight => _ftHexDigits.Height;

        /// <summary>
        /// Gets the width of a single digit "0"
        /// </summary>
        public double SingleDigitWidth => _ftDigit.Width;

        /// <summary>
        /// Gets the width of two hex digits
        /// </summary>
        public double HexDigitPairWidth => _ftHexDigits.Width;

        /// <summary>
        /// Gets the width of two hex digits and the trailing space "00 "
        /// </summary>
        public double HexDigitPairWidthIncludingSpace => _ftHexDigits.Width + _ftSpace.WidthIncludingTrailingWhitespace;

        /// <summary>
        /// Width of a character in the ASCII section "."
        /// </summary>
        public double AsciiCharWidth => _ftAscii.Width;

        /// <summary>
        /// Width of a space
        /// </summary>
        public double SpaceWidth => _ftSpace.WidthIncludingTrailingWhitespace;

        /// <summary>
        /// Half of the width of a spcae
        /// </summary>
        public double HalfSpaceWidth => this.SpaceWidth / 2;

        #endregion
    }

}
