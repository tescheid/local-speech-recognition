using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PAIND_Communication
{
    public class AudioEventArgs:EventArgs
    {
        bool played = false;
        public AudioEventArgs(bool played) {
            this.played = played;
        } 
    }
}
