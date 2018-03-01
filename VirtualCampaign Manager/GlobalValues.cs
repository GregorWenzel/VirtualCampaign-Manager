using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager
{
    public enum UpdateType
    {
        Status,
        ErrorCode,
        OutputExtension,
        Film,
        Priority,
        RenderID
    };

    public static class GlobalValues
    {
        private static int renderQueueCount = 0;

        public static int RenderQueueCount { get => renderQueueCount; set => renderQueueCount = value; }
    }
}
