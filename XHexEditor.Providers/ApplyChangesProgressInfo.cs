using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers
{
    public record ApplyChangesProgressInfo(ApplyChangesStatus Status, long BytesRemaining);

    public enum ApplyChangesStatus
    {
        Preparing,
        Applying
    }
}
