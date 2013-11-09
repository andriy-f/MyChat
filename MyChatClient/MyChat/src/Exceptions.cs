/***********************************************************
 *
 *   2009
 *
 *   Description:   Specialized exceptions that can be thrown while
 *                  parsing or differentiating a parametric equation.
 *   Created:       3/10/09
 *   Author:        Andriy Fetsyuk (Andreus)
 *
 ************************************************************/

//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

namespace Andriy.MyChat.Client
{
    using System;

    class ChatClientException:Exception
    {
        public ChatClientException( string msg ) : base( msg )
        {
        }
        public ChatClientException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }
    }
}
