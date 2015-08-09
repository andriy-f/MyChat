namespace MyChat.Common
{
    public interface IMessageVisitor
    {
        void Visit(AbstractMessage message);
    }
}