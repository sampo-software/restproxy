using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using RestProxy.Net.Configuration;
using RestProxy.Net.DTO;
using RestProxy.Net.Repositories;
using App = RestProxy.Net.WebApiApplication;


namespace RestProxy.Net.Controllers
{
    [Route("api/sms")]
    public class SMSController : ApiController
    {
        public SMSController()
        {
        }


        /// <summary>
        /// Test call, returns RestProxy version, Twilio default gateway phone number and debug log mode
        /// </summary>
        [Route("api/sms/test")]
        [HttpGet]
        public HttpResponseMessage Test()
        {
            App.logger.Log("SMSController.Test(): Сlient test call");
            HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK, "Test OK. " + App.restConfiguration.Version + ". Debug mode is " + App.logger.DebugMode + ".");
            return resp;
        }


        /// <summary>
        /// Debug mode control for logging (log data or not)
        /// </summary>
        /// <param name="switch">"on", "true" or "1" to enable debugging log entries </param>
        /// <returns>HttpStatusCode OK and the text string specifying current debug level</returns>
        [Route("api/sms/debug/{debugmode?}")]
        [HttpGet]
        public HttpResponseMessage Debug(string debugmode = "")
        {
            string now = "";

            if (!string.IsNullOrWhiteSpace(debugmode))
            {
                App.logger.Log("SMSController.Debug(): Client request to change debug mode to '" + debugmode + "'");
                App.logger.DebugMode = debugmode;
                now = "now ";
            }

            HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.OK, "Debug mode is " + now + App.logger.DebugMode);
            return resp;
        }


        /// <summary>
        /// Handles incoming MMS message from client (Netzoom app?)
        /// </summary>
        /// <param name="to">Recipient phone number (without + sign)</param>
        /// <param name="body">Message body</param>
        /// <param name="msg">Message body</param>
        /// <param name="mediaurls">Set of URLS pointing to media files. Note: URL-Encoded comma-separated set of URL-Endcoded URLs (two-pass URL-Encode!)</param>
        /// <param name="gatewaynum">Optional client-specified gateway phone number (without + sign)</param>
        /// <returns>Http Status code of Twilio send API</returns>
        [HttpGet]
        [Route("api/mms/send/{to}/{body}/{mediaurls}/{gatewaynum?}")]
        public async Task<HttpResponseMessage> ReceiveMmsMessageFromClient(string to, string body, string mediaurls, string gatewaynum = "")
        {
            // DEBUG
            App.logger.Log("SMSController.ReceiveMessageFromClient(): New outbox message to " + to);

            if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(body))
                return new HttpResponseMessage(HttpStatusCode.NoContent);

            string from = App.restConfiguration.TwilioPhoneNumber;

            if (!string.IsNullOrWhiteSpace(gatewaynum))
            {
                from = gatewaynum;
                App.logger.Log("SMSController.ReceiveMessageFromClient(): Client specified gateway phone number +" + from);
            }

            OutgoingTwilioMessage msg = new OutgoingTwilioMessage()
            {
                From = $"+{from}",      // From phone number (gateway number via which the message will be sent)
                To = $"+{to}",          // Destination phone number (any recipient's mobile number)
                Action = "",            // unused
                Method = "",            // unused
                Body = body,            // Message body
                MediaURLs = mediaurls,  // URL-Encoded comma-separated set of URL-Endcoded URLs (two-pass URL-Encode!)</param>
                Client = ""             // Client identofier, unused here, but can be relevant for incoming messages if implemented
            };

            try
            {
                App.messageRepository.StoreIncomingMessageFromClient(msg);
                HttpResponseMessage response = await ForwardMessageToTwilio(msg);
                return response;
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }
            finally
            { }
        }


        /// <summary>
        /// Handles incoming nmessage from client (Netzoom app?)
        /// </summary>
        /// <param name="to">Recipient phone number (without + sign)</param>
        /// <param name="body">Message body</param>
        /// <param name="gatewaynum">Optional client-specified gateway phone number (without + sign)</param>
        /// <returns>Http Status code of Twilio send API</returns>
        [HttpGet]
        [Route("api/sms/send/{to}/{body}/{gatewaynum?}")]
        public async Task<HttpResponseMessage> ReceiveSmsMessageFromClient(string to, string body, string gatewaynum = "")
        {
            // DEBUG
            App.logger.Log("SMSController.ReceiveMessageFromClient(): New outbox message to +" + to);

            if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(body))
                return new HttpResponseMessage(HttpStatusCode.NoContent);

            string from = App.restConfiguration.TwilioPhoneNumber;

            if (!string.IsNullOrWhiteSpace(gatewaynum))
            {
                from = gatewaynum;
                App.logger.Log("SMSController.ReceiveMessageFromClient(): Client specified gateway phone number +" + from);
            }

            OutgoingTwilioMessage msg = new OutgoingTwilioMessage()
            {
                From = $"+{from}",  // From phone number (gateway number via which the message will be sent)
                To = $"+{to}",      // Destination phone number (any recipient's mobile number)
                Action = "",        // unused
                Method = "",        // unused
                Body = body,        // Message body
                MediaURLs = "",     // unused in SMS mode
                Client = ""         // Client identofier, unused here, but can be relevant for incoming messages if implemented
            };

            try
            {
                App.messageRepository.StoreIncomingMessageFromClient(msg);
                HttpResponseMessage response = await ForwardMessageToTwilio(msg);
                return response;
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }
            finally
            { }
        }


        /// <summary>
        /// Gets the particular message (by MessageSid) from Twilio - useful to check delivery status of sent message
        /// </summary>
        /// <param name="messagesid">Message identifier</param>
        /// <returns>Twilio response with message</returns>
        [HttpGet]
        [Route("api/sms/message/{messagesid}")]
        public async Task<HttpResponseMessage> GetMessage(string messagesid)
        {
            // DEBUG
            App.logger.Log("SMSController.GetMessage(): Client request message id '" + messagesid + "'");

            if (string.IsNullOrWhiteSpace(messagesid) || string.IsNullOrWhiteSpace(messagesid))
                return new HttpResponseMessage(HttpStatusCode.NoContent);

            try
            {
                HttpResponseMessage response = await GetMessageFromTwilio(messagesid);
                return response;
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }
            finally
            { }
        }




        // Documentation for Twilio sending API: https://www.twilio.com/docs/api/rest/request
        //
        private async Task<HttpResponseMessage> ForwardMessageToTwilio(OutgoingTwilioMessage msg)
        {
            // DEBUG
            App.logger.Log("SMSController.ForwardMessageToTwilio(): Forwarding message via Twilio (" + msg.From + ") to  " + msg.To + " , ID=" + msg.id);

            using (var client = new HttpClient { BaseAddress = new Uri(App.restConfiguration.TwilioBaseUrl) })
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(Encoding.ASCII.GetBytes($"{App.restConfiguration.TwilioAccountsid}:{App.restConfiguration.TwilioToken}")));

                string requestUri = $"/2010-04-01/Accounts/{App.restConfiguration.TwilioAccountsid}/Messages";

                // POST parameters for Twilio 
                var keyValues = new List<KeyValuePair<string, string>>();

                // Basic SMS content
                keyValues.Add(new KeyValuePair<string, string>("To", msg.To));
                keyValues.Add(new KeyValuePair<string, string>("From", msg.From));
                keyValues.Add(new KeyValuePair<string, string>("Body", msg.Body));

                // Optional MMS media URL(s)
                if (!String.IsNullOrEmpty(msg.MediaURLs))
                {
                    // UrlDecode first pass -- allow delimiters to work
                    var mediaUrlsDecoded1 = WebUtility.UrlDecode(msg.MediaURLs);

                    App.logger.Log("SMSController.ForwardMessageToTwilio(): MMS mode: MediaUrls UrlDecoded 1st pass is '" + mediaUrlsDecoded1 + "'");

                    // Delimit decoded mediaUrls to a set of mediaUrls
                    var mediaUrls = mediaUrlsDecoded1.Split(',');

                    if (mediaUrls.Count() > 0)
                    {
                        foreach (var mediaUrl in mediaUrls)
                        {
                            // Add each media URL after UrlDecode (second pass)
                            var mediaUrlDecoded2 = WebUtility.UrlDecode(mediaUrl);  // This is possibly not needed (1st pass decode takes care about it)
                            App.logger.Log("SMSController.ForwardMessageToTwilio(): Adding UrlDecoded 2nd pass MediaUrl '" + mediaUrlDecoded2 + "'");
                            keyValues.Add(new KeyValuePair<string, string>("MediaUrl", mediaUrlDecoded2));
                        }
                    }
                    else
                    {
                        App.logger.Log("SMSController.ForwardMessageToTwilio(): MediaUrl format error, unable to parse MediaUrls. Will use SMS mode.");
                    }
                }

                var content = new FormUrlEncodedContent(keyValues);
                HttpResponseMessage response = new HttpResponseMessage();

                try
                {
                    response = await client.PostAsync(requestUri, content);
                }
                catch (HttpRequestException hre)
                {
                    App.logger.Log("SMSController.ForwardMessageToTwilio(): Http request to Twilio failed: " + hre.Message + " (" + hre.InnerException.Message + ")");

                    // Generate new response on behalf of failed connected endpoint
                    HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.GatewayTimeout, "Message sending failed: " + hre.Message + " (" + hre.InnerException.Message + ")");
                    return resp;
                }

                if (response.IsSuccessStatusCode)
                {
                    App.messageRepository.DeleteOutboxMessage(msg.id);  // Sent successfully via Twilio, delete the message from local Outbox database
                    App.logger.Log("SMSController.ForwardMessageToTwilio(): Sent successfully via Twilio with status '" + response.StatusCode + "'");
                }
                else
                {
                    // DEBUG
                    App.logger.Log("SMSController.ForwardMessageToTwilio(): Forwarding to Twilio failed with status '" + response.StatusCode + "'");

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        App.logger.Log("SMSController.ForwardMessageToTwilio(): Deleting invalid outbox message");
                        App.messageRepository.DeleteOutboxMessage(msg.id);
                    }

                }

                // Forward Twilio returned status code to the client
                return response;
            }
        }


        // Documentation for Twilio Message Resource:
        // https://www.twilio.com/docs/api/messaging/message#message-status-values
        //
        private async Task<HttpResponseMessage> GetMessageFromTwilio(string messagesid)
        {
            // DEBUG
            App.logger.Log("SMSController.GetMessageFromTwilio(): message from Twilio MessageSid '" + messagesid + "'");

            using (var client = new HttpClient { BaseAddress = new Uri(App.restConfiguration.TwilioBaseUrl) })
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(Encoding.ASCII.GetBytes($"{App.restConfiguration.TwilioAccountsid}:{App.restConfiguration.TwilioToken}")));

                string requestUri = $"/2010-04-01/Accounts/{App.restConfiguration.TwilioAccountsid}/Messages/{messagesid}";

                HttpResponseMessage response = new HttpResponseMessage();

                try
                {
                    response = await client.GetAsync(requestUri);
                }
                catch (HttpRequestException hre)
                {
                    App.logger.Log("SMSController.GetMessageFromTwilio(): Http request to Twilio failed: " + hre.Message + " (" + hre.InnerException.Message + ")");

                    // Generate new response on behalf of failed connected endpoint
                    HttpResponseMessage resp = Request.CreateResponse(HttpStatusCode.GatewayTimeout, "Message sending failed: " + hre.Message + " (" + hre.InnerException.Message + ")");
                    return resp;
                }

                if (response.IsSuccessStatusCode)
                {
                    App.logger.Log("SMSController.GetMessageFromTwilio(): Get message successfully from Twilio with status '" + response.StatusCode + "'");
                }
                else
                {
                    // DEBUG
                    App.logger.Log("SMSController.GetMessageFromTwilio(): Get message from Twilio failed with status '" + response.StatusCode + "'");
                }

                // Forward Twilio returned status code to the client
                return response;
            }
        }


        /// <summary>
        /// WebHook for receiving massages from Twilio
        /// {url}/api/twilio/sms/receive
        /// </summary>
        /// <param name="AccountSid"></param>
        /// <param name="ApiVersion"></param>
        /// <param name="Body"></param>
        /// <param name="From"></param>
        /// <param name="FromCity"></param>
        /// <param name="FromCountry"></param>
        /// <param name="FromState"></param>
        /// <param name="FromZip"></param>
        /// <param name="MessageSid"></param>
        /// <param name="NumMedia"></param>
        /// <param name="NumSegments"></param>
        /// <param name="SmsSid"></param>
        /// <param name="SmsStatus"></param>
        /// <param name="To"></param>
        /// <param name="ToCity"></param>
        /// <param name="ToCountry"></param>
        /// <param name="ToState"></param>
        /// <param name="ToZip"></param>
        /// <returns>Status code</returns>
        [HttpPost]
        [Route("api/sms/receive")]
        public HttpResponseMessage Receive(FormDataCollection formData )
        {
            // DEBUG
            App.logger.Log("SMSController.Receive(): receiving message from Twilio");

            IncomingTwilioMessage msgIn = new IncomingTwilioMessage()
            {
                Timestamp = DateTime.Now,
                AccountSid = formData["AccountSid"] ?? "",
                ApiVersion = formData["ApiVersion"] ?? "",
                Body = formData["Body"] ?? "",
                From = formData["From"] ?? "",
                FromCity = formData["FromCity"] ?? "",
                FromCountry = formData["FromCountry"] ?? "",
                FromState = formData["FromState"] ?? "",
                FromZip = formData["FromZip"] ?? "",
                MessageSid = formData["MessageSid"] ?? "",
                NumMedia = formData["NumMedia"] ?? "",
                NumSegments = formData["NumSegments"] ?? "",
                SmsSid = formData["SmsSid"] ?? "",
                SmsStatus = formData["SmsStatus"] ?? "",
                To = formData["To"] ?? "",
                ToCity = formData["ToCity"] ?? "",
                ToCountry = formData["ToCountry"] ?? "",
                ToState = formData["ToState"] ?? "",
                ToZip = formData["ToZip"] ?? "",
                MediaURLs = ""
            };

            // DEBUG
            App.logger.Log("SMSController.Receive(): new message received via Twilio from " + msgIn.From + " to " + msgIn.To);

            App.messageRepository.StoreIncomingMessageFromTwilio(msgIn);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }


        /// <summary>
        /// Handles queries for client messages
        /// </summary>
        /// <param name="client"></param>
        /// <param name="select"></param>
        /// <param name="afterdate"></param>
        /// <param name="befordate"></param>
        /// <returns>List of matching messages</returns>
        [HttpGet]
        [Route("api/sms/list/{select?}/{client?}/{afterdate?}/{beforedate?}")]      // Optional parameters with "?" sign
        public HttpResponseMessage ListClientMessages(string select = "new", string client = "-", string afterdate = "", string befordate = "")
        {
            // DEBUG
            App.logger.Log("SMSController.ListClientMessages(): Processing '" + select + "' command");

            List<IncomingTwilioMessage> msgs = new List<IncomingTwilioMessage>();

            switch(select)
            {
                case "new":
                    msgs = App.messageRepository.GetAllClientMessages(client);
                    break;
                case "before":
                    msgs = App.messageRepository.GetClientMessagesBefore(client, befordate);
                    break;
                case "between":
                    msgs = App.messageRepository.GetClientMessagesBetween(client, befordate, afterdate);
                    break;
                case "after":
                    msgs = App.messageRepository.GetClientMessagesAfter(client, afterdate);
                    break;
                default:
                    // DEBUG
                    App.logger.Log("SMSController.ListClientMessages(): unknown command");

                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            // DEBUG
            App.logger.Log("SMSController.ListClientMessages(): Listing " + msgs.Count + " inbox messages for client '" + client + "'");


            if (msgs.Count == 0)
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            else
            {
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, msgs);

                // TODO: How to check if the response with the messages(s) has been succesfully sent to client?
                // Only then we should delete all received messages

                foreach (var msg in msgs)
                    App.messageRepository.DeleteInboxMessage(msg.id);

                return response;
            }
        }


        /// <summary>
        /// Call to the service for checking own pending (previously failed) outbox messages and re-sending them to Twilio
        /// </summary>
        [Route("api/sms/resend")]
        [HttpGet]
        public async Task<HttpResponseMessage> ResendOutboxMessages()
        {
            App.logger.Log("SMSController.ResendOutboxMessages()");

            var pendingMessages = App.dbstore.GetAllOutboxMessages();
            var result = new HttpResponseMessage();

            if (pendingMessages.Count == 0)
            {
                App.logger.Log("SMSController.ResendOutboxMessages(): No pending messages, exiting");
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            App.logger.Log("SMSController.ResendOutboxMessages(): " + pendingMessages.Count + " Pending messages to re-send");

            foreach (var msg in pendingMessages)
            {
                App.logger.Log("SMSController.ResendOutboxMessages(): Re-sending message ID=" + msg.id);
                result = await ForwardMessageToTwilio(msg);
                App.logger.Log("SMSController.ResendOutboxMessages(): Re-sent message ID=" + msg.id + " with status " + result.StatusCode);
            }

            App.logger.Log("SMSController.ResendOutboxMessages(): Finished");
            return result;
        }
    }
}