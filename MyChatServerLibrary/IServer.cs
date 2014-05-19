namespace Andriy.MyChat.Server
{
    using System.Collections.Generic;

    public interface IServer
    {
        IEnumerable<ChatClient> GetChatClients();

        ChatClient GetChatClient(string login);

        RoomParams GetRoom(string name);

        IEnumerable<string> GetRoomsNames();

        void StageClientForRemoval(ChatClient client);

        bool RoomExist(string room);

        bool ConfirmRoomPass(string roomName, string password);

        void AddUserToRoom(string room, string login);

        bool TryCreateRoom(string name, string password);

        void RemoveClientFromRoom(string login, string room);

        byte[] FormatRoomUsers(string room);
    }
}