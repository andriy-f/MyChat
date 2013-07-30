using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyChat
{
    class ListenProcessor
    {
        public int code;//code to wait for
        public Action toDo;//is performed on receiving code

        public ListenProcessor(int newcode, Action newtoDo)
        {
            code = newcode;
            toDo = newtoDo;
        }
    
    }
}
