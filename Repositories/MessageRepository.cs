using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestProxy.Net.DTO;
using App = RestProxy.Net.WebApiApplication;

namespace RestProxy.Net.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        public List<IncomingTwilioMessage> GetAllClientMessages(string client)
        {
            return App.dbstore.GetAllInboxMessages();
        }

        public List<IncomingTwilioMessage> GetClientMessagesAfter(string client, string afterdate)
        {
            App.logger.Log("DBStore.GetClientMessagesAfter(): unimplemented");
            return GenerateDemoMessageList();
        }

        public List<IncomingTwilioMessage> GetClientMessagesBefore(string client, string befordate)
        {
            App.logger.Log("DBStore.GetClientMessagesBefore(): unimplemented");
            return GenerateDemoMessageList();
        }

        public List<IncomingTwilioMessage> GetClientMessagesBetween(string client, string befordate, string afterdate)
        {
            App.logger.Log("DBStore.GetClientMessagesBetween(): unimplemented");
            return GenerateDemoMessageList();
        }

        public List<IncomingTwilioMessage> GetNewClientMessages(string client)
        {
            return App.dbstore.GetAllInboxMessages();
        }

        public void StoreIncomingMessageFromClient(OutgoingTwilioMessage msg)
        {
            App.dbstore.StoreOutboxMessage(msg);
        }

        public void StoreIncomingMessageFromTwilio(IncomingTwilioMessage msg)
        {
            App.dbstore.StoreInboxMessage(msg);
        }

        public void DeleteInboxMessage(int msgId)
        {
            App.dbstore.DeleteInboxMessage(msgId);
        }

        public void DeleteOutboxMessage(int msgId)
        {
            App.dbstore.DeleteOutboxMessage(msgId);
        }


        private List<IncomingTwilioMessage> GenerateDemoMessageList()
        {
            return new List<IncomingTwilioMessage>()
            {
                new IncomingTwilioMessage() { From="From1", Body="THIS CALL IS NOT IMPLEMENTED"},
                new IncomingTwilioMessage() { From="From2", Body="PLEASE SEE THE LOG"},
                new IncomingTwilioMessage() { From="From3", Body="FOR DETAILS"},
            };
        }
    }
}
