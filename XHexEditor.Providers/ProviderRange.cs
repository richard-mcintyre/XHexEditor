using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers
{    
    public record ProviderRange(long StartIndex /*inclusive*/, long EndIndex /*inclusive*/)
    {
        public long Count
        {
            get
            {
                if (this.StartIndex <= this.EndIndex)
                {
                    return Math.Max(1, (this.EndIndex - this.StartIndex) + 1);
                }
                else
                {
                    return (this.StartIndex - this.EndIndex) + 1;
                }
            }
        }

        public ProviderRange Normalize()
        {
            if (this.StartIndex <= this.EndIndex)
            {
                return this;
            }
            else
            {
                return new ProviderRange(this.EndIndex, this.StartIndex);
            }
        }

        public bool IsInRange(long index)
        {
            ProviderRange range = Normalize();
            return RangesOverlap(range.StartIndex, range.EndIndex, index, index);
        }

        private static bool RangesOverlap(long range1Start, long range1End, long range2Start, long range2End)
        {
            if (range1Start <= range2Start && range2Start <= range1End ||
                range1Start <= range2End && range2End <= range1End)
                return true;

            if (range2Start <= range1Start && range1Start <= range2End ||
                range2Start <= range1End && range1End <= range2End)
                return true;

            return false;
        }
    }
}
