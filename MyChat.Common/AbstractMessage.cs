namespace MyChat.Common
{
    public abstract class AbstractMessage
    {
        public virtual void Accept(IMessageVisitor visitor)
        {
            visitor.Visit((dynamic)this);
        }
    }
}