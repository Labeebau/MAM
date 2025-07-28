using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAM
{
    public enum ProcessType
    {
        ThumbnailGeneration,
        ProxyGeneration,
        Transcoding,
        QualityCheck,
        FileCopying,
        DownloadOriginalFile,
        DownloadProxy,
        MetadataExtraction
    }

}
