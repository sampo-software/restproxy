using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlServerCe;
using System.Data.SqlTypes;
// using System.Data.Common;
using System.Net;
using System.Net.Http;
using RestProxy.Net;
using App = RestProxy.Net.WebApiApplication;
using RestProxy.Net.DTO;

namespace RestProxy.Net.Repositories
{
    /* 
     * 
     * Local MS SQL Compact Edition database for storing inbox and outbox messages
     * 
     */
    public class DBStore
    {
        private SqlCeConnection dbConnection = null;
        string DBname = "RestProxyNet.sdf";
        string TableInbox = "Inbox";
        string TableOutbox = "Outbox";
        public bool isConnected = false;


        public DBStore()
        {
            bool databaseFound = true;
            //string localDbFile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + DBname;    // "MyDocuments" does not resolve on IIS
            string localDbFile = "C:\\RestProxy\\" + DBname;

            App.logger.Log("DBStore: initiating local DB (file = " + localDbFile + ")");

            if (!System.IO.File.Exists(localDbFile))
            {
                System.IO.File.Create(localDbFile).Close(); // Create empty file for MS SQL CE db if not exists
                databaseFound = false;
            }

            try
            {
                dbConnection = new SqlCeConnection();
                dbConnection.ConnectionString = "Data Source = " + localDbFile;
                dbConnection.Open();
            }
            catch (Exception ex)
            {
                var message = "! Error in DBStore.DBStore(): " + ex.Message + "\n" + ex.TargetSite;
                App.logger.Log(message);

                return;
            }

            // Connected OK, now will do an additional check
            if (dbConnection.ServerVersion != "")
                isConnected = true;

            if (!databaseFound)
                CreateLocalDatabase();
        }
        

        public int StoreOutboxMessage(OutgoingTwilioMessage msgout)
        {
            int id = 0; // ID of the inserted SQL CE record
            
            // This results to
            // INSERT INTO User (FirstName, LastName) VALUES ('test','test'),('test','test'),... ;
            SqlCeCommand cmd = dbConnection.CreateCommand();
            cmd.CommandText = TableOutbox;
            cmd.CommandType = CommandType.TableDirect;
            SqlCeResultSet rs = cmd.ExecuteResultSet(ResultSetOptions.Updatable | ResultSetOptions.Scrollable);
            SqlCeUpdatableRecord rec = rs.CreateRecord();

            try
            {
                // DEBUG
                /*
                App.logger.Log("DBStore.StoreDataEntries(): Storing new entry:\n" + 
                            "\ttimestamp: " + qlmActivation.Timestamp + 
                            "\tkey: " + qlmActivation.LicenseKey +
                            "\texpiration: " + qlmActivation.ExpirationDateTime);
                */

                rec.SetSqlDateTime(1, DateTime.Now);
                rec.SetString(2, msgout.From);
                rec.SetString(3, msgout.To);
                rec.SetString(4, msgout.Action);
                rec.SetString(5, msgout.Method);
                rec.SetString(6, msgout.Body);
                rec.SetString(7, msgout.Client);
                rs.Insert(rec);

                // Get this inserter record ID
                cmd.CommandText = "SELECT @@IDENTITY";
                cmd.CommandType = CommandType.Text;
                id = Convert.ToInt32(cmd.ExecuteScalar());

                msgout.id = id; // Assign id to this message for further referencing from message to its DBStore record

                // DEBUG
                App.logger.Log("DBStore.StoreOutboxMessage() storing outbox message from " + msgout.From + " to " + msgout.To + ", ID=" + id);
            }
            catch (Exception ex)
            {
                var message = "! Error in DBStore.StoreOutboxMessage(): " + ex.Message + "\n" + ex.TargetSite;
                App.logger.Log(message);
            }

            cmd.Dispose();

            return id;
        }


        public bool StoreInboxMessage(IncomingTwilioMessage msgin)
        {
            bool retVal = true;

            int id = 0; // ID of the inserted SQL CE record

            // This results to
            // INSERT INTO User (FirstName, LastName) VALUES ('test','test'),('test','test'),... ;
            SqlCeCommand cmd = dbConnection.CreateCommand();
            cmd.CommandText = TableInbox;
            cmd.CommandType = CommandType.TableDirect;
            SqlCeResultSet rs = cmd.ExecuteResultSet(ResultSetOptions.Updatable | ResultSetOptions.Scrollable);
            SqlCeUpdatableRecord rec = rs.CreateRecord();

            try
            {
                // DEBUG
                /*
                App.logger.Log("DBStore.StoreDataEntries(): Storing new entry:\n" + 
                            "\ttimestamp: " + qlmActivation.Timestamp + 
                            "\tkey: " + qlmActivation.LicenseKey +
                            "\texpiration: " + qlmActivation.ExpirationDateTime);
                */

                rec.SetSqlDateTime(1, msgin.Timestamp);
                rec.SetString(2, msgin.AccountSid);
                rec.SetString(3, msgin.ApiVersion);
                rec.SetString(4, msgin.Body);
                rec.SetString(5, msgin.From);
                rec.SetString(6, msgin.FromCity);
                rec.SetString(7, msgin.FromCountry);
                rec.SetString(8, msgin.FromState);
                rec.SetString(9, msgin.FromZip);
                rec.SetString(10, msgin.MessageSid);
                rec.SetString(11, msgin.NumMedia);
                rec.SetString(12, msgin.NumSegments);
                rec.SetString(13, msgin.SmsSid);
                rec.SetString(14, msgin.SmsStatus);
                rec.SetString(15, msgin.ToState);
                rec.SetString(16, msgin.To);
                rec.SetString(17, msgin.ToCity);
                rec.SetString(18, msgin.ToCountry);
                rec.SetString(19, msgin.ToZip);
                rec.SetString(20, msgin.MediaURLs);
                rs.Insert(rec);

                // Get this inserter record ID
                cmd.CommandText = "SELECT @@IDENTITY";
                cmd.CommandType = CommandType.Text;
                id = Convert.ToInt32(cmd.ExecuteScalar());

                // DEBUG
                App.logger.Log("DBStore.StoreInboxMessage() storing inbox message from '" + msgin.From + "' to '" + msgin.To + "', ID=" + id);
            }
            catch (Exception ex)
            {
                var message = "! Error in DBStore.StoreInboxMessage(): " + ex.Message + "\n" + ex.TargetSite;
                App.logger.Log(message);

                retVal = false;
            }

            cmd.Dispose();

            return retVal;
        }


        public bool DeleteOutboxMessage(int id)
        {
            bool retVal = true;

            SqlCeCommand cmd = dbConnection.CreateCommand();

            try
            {
                cmd.CommandText = "DELETE FROM " + TableOutbox + " WHERE ID = @id";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@id", id);
                //cmd.Parameters.AddWithValue("@activationTimestamp", SqlDateTime);
                //cmd.Parameters.Add("@activationTimestamp", SqlDbType.Timestamp).Value = qlmActivation.Timestamp;

                var status = cmd.ExecuteNonQuery();

                // DEBUG
                App.logger.Log("DBStore.DeleteOutboxMessage(): DELETE message ID=" + id + ", status = " + status);
            }
            catch (Exception ex)
            {
                var message = "! Error in DBStore.DeleteOutboxMessage(): " + ex.Message + "\n" + ex.TargetSite;
                App.logger.Log(message);

                retVal = false;
            }

            cmd.Dispose();

            return retVal;
        }


        public bool DeleteInboxMessage(int id)
        {
            bool retVal = true;

            SqlCeCommand cmd = dbConnection.CreateCommand();

            try
            {
                cmd.CommandText = "DELETE FROM " + TableInbox + " WHERE ID = @id";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@id", id);
                //cmd.Parameters.AddWithValue("@activationTimestamp", SqlDateTime);
                //cmd.Parameters.Add("@activationTimestamp", SqlDbType.Timestamp).Value = qlmActivation.Timestamp;

                var status = cmd.ExecuteNonQuery();

                //DEBUG
                App.logger.Log("DBStore.DeleteInboxMessage(): DELETE message ID=" + id + ", status = " + status);
            }
            catch (Exception ex)
            {
                var message = "! Error in DBStore.DeleteInboxMessage(): " + ex.Message + "\n" + ex.TargetSite;
                App.logger.Log(message);

                retVal = false;
            }

            cmd.Dispose();

            return retVal;
        }


        public List<OutgoingTwilioMessage> GetAllOutboxMessages()
        {
            List<OutgoingTwilioMessage> outboxMsgs = new List<OutgoingTwilioMessage>();

            SqlCeCommand cmd = dbConnection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM " + TableOutbox;

            Int32 count = (Int32)cmd.ExecuteScalar();
            if (count == 0)
            {
                App.logger.Log("DBStore.GetAllOutboxMessages(): no messages to offload");
                return outboxMsgs;
            }

            // DEBUG
            App.logger.Log("DBStore.GetAllOutboxMessages(): offloading " + count + " outbox message(s)");

            try
            {
                //cmd.CommandText = "SELECT TOP (" + maxBatchLoad + ") * FROM " + TableActivations;
                cmd.CommandText = "SELECT * FROM " + TableOutbox;
                SqlCeResultSet rs = cmd.ExecuteResultSet(ResultSetOptions.Updatable | ResultSetOptions.Scrollable);

                while (rs.Read())
                {
                    OutgoingTwilioMessage outMsg = new OutgoingTwilioMessage
                    {
                        id = rs.GetInt32(0),
                        Timestamp = rs.GetDateTime(1),
                        From = rs.GetString(2),
                        To = rs.GetString(3),
                        Action = rs.GetString(4),
                        Method = rs.GetString(5),
                        Body = rs.GetString(6),
                        MediaURLs = rs.GetString(7),
                        Client = rs.GetString(8)
                    };

                    // DEBUG
                    App.logger.Log("DBStore.GetAllOutboxMessages(): message = " + outMsg.ToString());

                    outboxMsgs.Add(outMsg);
                }
            }
            catch (Exception ex)
            {
                var message = "! Error in DBStore.GetAllOutboxMessages(): " + ex.Message + "\n" + ex.TargetSite;
                App.logger.Log(message);
            }

            return outboxMsgs;
        }


        public List<IncomingTwilioMessage> GetAllInboxMessages()
        {
            List<IncomingTwilioMessage> inboxMsgs = new List<IncomingTwilioMessage>();

            SqlCeCommand cmd = dbConnection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM " + TableInbox;

            Int32 count = (Int32)cmd.ExecuteScalar();
            if (count == 0)
            {
                App.logger.Log("DBStore.GetAllInboxMessages(): no messages to offload");
                return inboxMsgs;
            }

            App.logger.Log("DBStore.GetAllInboxMessages(): offloading " + count + " inbox message(s)");

            try
            {
                //cmd.CommandText = "SELECT TOP (" + maxBatchLoad + ") * FROM " + TableActivations;
                cmd.CommandText = "SELECT * FROM " + TableInbox;
                SqlCeResultSet rs = cmd.ExecuteResultSet(ResultSetOptions.Updatable | ResultSetOptions.Scrollable);

                while (rs.Read())
                {
                    IncomingTwilioMessage inMsg = new IncomingTwilioMessage
                    {
                        id = rs.GetInt32(0),
                        Timestamp = rs.GetDateTime(1),
                        AccountSid = rs.GetString(2),
                        ApiVersion = rs.GetString(3),
                        Body = rs.GetString(4),
                        From = rs.GetString(5),
                        FromCity = rs.GetString(6),
                        FromCountry = rs.GetString(7),
                        FromState = rs.GetString(8),
                        FromZip = rs.GetString(9),
                        MessageSid = rs.GetString(10),
                        NumMedia = rs.GetString(11),
                        NumSegments = rs.GetString(12),
                        SmsSid = rs.GetString(13),
                        SmsStatus = rs.GetString(14),
                        ToState = rs.GetString(15),
                        To = rs.GetString(16),
                        ToCity = rs.GetString(17),
                        ToCountry = rs.GetString(18),
                        ToZip = rs.GetString(19),
                        MediaURLs = rs.GetString(20)
                    };

                    // DEBUG
                    App.logger.Log("DBStore.GetAllInboxMessages(): message = " + inMsg.ToString());

                    inboxMsgs.Add(inMsg);
                }
            }
            catch (Exception ex)
            {
                var message = "! Error in DBStore.GetAllInboxMessages(): " + ex.Message + "\n" + ex.TargetSite;
                App.logger.Log(message);
            }

            return inboxMsgs;
        }


        public void CreateLocalDatabase()
        {
            App.logger.Log("DBStore: local DB not found, initialising...");

            SqlCeCommand cmdCreateTable = dbConnection.CreateCommand();
            cmdCreateTable.CommandText = "CREATE TABLE " + TableOutbox + "(" +
                "ID INT IDENTITY NOT NULL PRIMARY KEY, " +
                "Timestamp DATETIME," +
                "Fromx NTEXT," +
                "Tox NTEXT," +
                "Action NTEXT," +
                "Method NTEXT," +
                "Body NTEXT," +
                "MediaURLs NTEXT," +
                "Client NTEXT)";

            cmdCreateTable.ExecuteNonQuery();
            App.logger.Log("DBStore: creating table " + TableOutbox);

            cmdCreateTable = dbConnection.CreateCommand();
            cmdCreateTable.CommandText = "CREATE TABLE " + TableInbox + "(" +
                "ID INT IDENTITY NOT NULL PRIMARY KEY, " + 
                "Timestamp DATETIME," +
                "AccountSid NTEXT," +
                "ApiVersion NTEXT," +
                "Body NTEXT," +
                "Fromx NTEXT," +
                "FromCity NTEXT," +
                "FromCountry NTEXT," +
                "FromState NTEXT," +
                "FromZip NTEXT," +
                "MessageSid NTEXT," +
                "NumMedia NTEXT," +
                "NumSegments NTEXT," +
                "SmsSid NTEXT," +
                "SmsStatus NTEXT," +
                "ToState NTEXT," +
                "Tox NTEXT," +
                "ToCity NTEXT," +
                "ToCountry NTEXT," +
                "ToZip NTEXT," +
                "MediaURLs NTEXT)";

            cmdCreateTable.ExecuteNonQuery();
            cmdCreateTable.Dispose();

            App.logger.Log("DBStore: creating table " + TableInbox);
        }


        public void Disconnect()
        {
            App.logger.Log("DBStore: local DB closed");
            dbConnection.Close();
        }
    }
}