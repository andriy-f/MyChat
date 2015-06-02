namespace Andriy.MyChat.Server
{
    using System.Collections.Generic;

    public interface IServer
    {
        IEnumerable<ClientEndpoint> GetChatClients();

        ClientEndpoint GetChatClient(string login);

        RoomParams GetRoom(string name);

        IEnumerable<string> GetRoomsNames();

        void StageClientForRemoval(ClientEndpoint clientEndpoint);

        bool RoomExist(string room);

        bool ConfirmRoomPass(string roomName, string password);

        void AddUserToRoom(string room, string login);

        bool TryCreateRoom(string name, string password);

        void RemoveClientFromRoom(string login, string room);

        byte[] FormatRoomUsers(string room);
    }
}