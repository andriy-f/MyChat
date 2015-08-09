namespace Andriy.MyChat.Client
{
    using System;

    class ListenProcessor
    {
        public int code; // Code to wait for
        public Action toDo; // Action that is performed on receiving code

        public ListenProcessor(int newcode, Action newtoDo)
        {
            this.code = newcode;
            this.toDo = newtoDo;
        }
    
    }
}
