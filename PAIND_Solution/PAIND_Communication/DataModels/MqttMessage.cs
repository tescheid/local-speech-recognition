using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PAIND_Communication.DataModels
{
    public class MqttMessage
    {
       public string BlindsAction { get; set; } = "";
        public string Sender { get; set; } = "";
    }
}
