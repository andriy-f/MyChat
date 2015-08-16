namespace Andriy.MyChat.Server
{
    using System;
    using System.Collections.Generic;

    using global::MyChat.Common.Models;

    public interface IServer
    {
        IEnumerable<ClientEndpoint> GetChatClients();

        ClientEndpoint GetChatClient(string login);

        RoomParams GetRoom(string name);

        IEnumerable<string> GetRoomsNames();

        IEnumerable<ChatRoomInfo> GetRoomInfos();

        void QueueClientForRemoval(ClientEndpoint clientEndpoint);

        bool RoomExist(string room);

        bool ConfirmRoomPass(string roomName, string password);

        void AddLoggedInUser(string login, ClientEndpoint clientEndpoint);

        bool IsLoggedIn(string login);

        void RemoveClient(string login);

        void AddUserToRoom(string room, string login);

        bool TryCreateRoom(string name, string password);

        void RemoveClientFromRoom(string login, string room);

        [Obsolete]
        byte[] FormatRoomUsers(string room);

        IEnumerable<string> GetRoomUsers(string roomName);
    }
}