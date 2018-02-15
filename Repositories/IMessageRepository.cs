using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestProxy.Net.DTO;

namespace RestProxy.Net.Repositories
{
    public interface IMessageRepository
    {
        void StoreIncomingMessageFromClient(OutgoingTwilioMessage msg);
        void StoreIncomingMessageFromTwilio(IncomingTwilioMessage msg);
        List<IncomingTwilioMessage> GetAllClientMessages(string client);
        List<IncomingTwilioMessage> GetClientMessagesAfter(string client, string afterdate);
        List<IncomingTwilioMessage> GetClientMessagesBefore(string client, string befordate);
        List<IncomingTwilioMessage> GetClientMessagesBetween(string client, string befordate, string afterdate);
        List<IncomingTwilioMessage> GetNewClientMessages(string client);
        void DeleteInboxMessage(int msgId);
        void DeleteOutboxMessage(int msgId);
    }
}
