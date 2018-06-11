using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualCampaign_Manager
{
    public class ResultEventArgs : EventArgs
    {
        public Object Result { get; set; }

        public ResultEventArgs(Object Result)
        {
            this.Result = Result;
        }
    }

    public class EventFireBase
    {
        public EventHandler<EventArgs> SuccessEvent;
        public EventHandler<ResultEventArgs> FailureEvent;

        public void FireSuccessEvent()
        {
            SuccessEvent?.Invoke(this, new EventArgs());
        }

        public void FireFailureEvent(object ResultObject = null)
        {
            FailureEvent?.Invoke(this, new ResultEventArgs(ResultObject));
        }
    }
}
