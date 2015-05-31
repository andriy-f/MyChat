namespace Andriy.MyChat.Client
{
    using System;

    class ListenProcessor
    {
        public int code;//code to wait for
        public Action toDo;//is performed on receiving code

        public ListenProcessor(int newcode, Action newtoDo)
        {
            this.code = newcode;
            this.toDo = newtoDo;
        }
    
    }
}
