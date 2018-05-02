using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Data
{
    public class RenderChunkStatus
    {
        public List<int> IndexList = new List<int>();
        public Job Job;
        public string SlaveName = "";
        public string Id = "";
        public int StartIndex = -1;
        public int EndIndex = -1;
        public int Status = -1;
        public int StartFrame = -1;
        public int EndFrame = -1;
        public int Processors = -1;
        public int ProcessorSpeed;
        public DateTime RenderStartDate;
        public DateTime RenderEndDate;
    }
}
