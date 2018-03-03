using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager.Workers
{
    public static class WorkerBase
    {
        public static EventHandler<EventArgs> SuccessEvent;
        public static EventHandler<EventArgs> FailureEvent;

        private static void FireSuccessEvent()
        {
            EventHandler<EventArgs> successEvent = SuccessEvent;
            if (successEvent != null)
            {
                successEvent(null, new EventArgs());
            }
        }

        private static void FireFailureEvent()
        {
            EventHandler<EventArgs> failureEvent = FailureEvent;
            if (failureEvent != null)
            {
                failureEvent(null, new EventArgs());
            }
        }

    }
}
