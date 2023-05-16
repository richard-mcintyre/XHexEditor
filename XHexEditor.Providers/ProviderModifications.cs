using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace XHexEditor.Providers
{
    public class ProviderModifications
    {
        #region Construction

        public ProviderModifications()
        {
        }

        #endregion

        #region Fields

        private long _currentProviderOffset;
        private readonly List<Tuple<long, long>> _list = new List<Tuple<long, long>>();

        #endregion

        #region Methods

        internal void SetCurrentProviderOffset(long offset) =>
            _currentProviderOffset = offset;

        internal void Add(long offset, long count) =>
            _list.Add(Tuple.Create(_currentProviderOffset + offset, count));

        public IEnumerable<Tuple<long, long>> GetModifications(long startIndex, long count)
        {
            foreach ((long curOffset, long curCount) in _list)
            {
                if (RangesOverlap(curOffset, curCount, startIndex, count))
                {
                    yield return Tuple.Create(curOffset, curCount);
                }
            }
        }

        private static bool RangesOverlap(long range1Start, long range1Count, long range2Start, long range2Count)
        {
            long range1End = range1Start + range1Count - 1;
            long range2End = range2Start + range2Count - 1;

            if (range1Start <= range2Start && range2Start <= range1End || 
                range1Start <= range2End && range2End <= range1End)
                return true;

            if (range2Start <= range1Start && range1Start <= range2End || 
                range2Start <= range1End && range1End <= range2End)
                return true;

            return false;
        }

        #endregion
    }
}
