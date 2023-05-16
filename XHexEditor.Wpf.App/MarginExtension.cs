using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace XHexEditor.Wpf.App
{
    public class MarginExtension : MarkupExtension
    {
        #region Construction

        static MarginExtension()
        {
            _margins.Add(MarginKind.DialogContent, new Thickness(5));
            _margins.Add(MarginKind.BetweenHorizontalButtons, new Thickness(3, 0, 0, 0));
            _margins.Add(MarginKind.BetweenRadioButtons, new Thickness(0, 3, 0, 0));
            _margins.Add(MarginKind.BetweenControls, new Thickness(0, 5, 0, 0));
            _margins.Add(MarginKind.InsideContainer, new Thickness(5));
        }

        public MarginExtension()
        {
        }

        public MarginExtension(MarginKind kind)
        {
            _kind = kind;
        }

        #endregion

        #region Fields

        private static readonly Dictionary<MarginKind, Thickness> _margins = new Dictionary<MarginKind, Thickness>();
        private static readonly Thickness _unknownMargin = new Thickness();

        private MarginKind _kind = MarginKind.Unknown;

        #endregion

        #region Properties

        public MarginKind Kind
        {
            get => _kind;
            set => _kind = value;
        }

        #endregion

        #region Methods

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (_margins.TryGetValue(Kind, out Thickness margin))
                return margin;

            return _unknownMargin;
        }

        #endregion
    }

    public enum MarginKind
    {
        Unknown,
        DialogContent,
        BetweenHorizontalButtons,
        BetweenRadioButtons,
        BetweenControls,
        InsideContainer,
    }
}
