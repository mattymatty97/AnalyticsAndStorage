using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json.Linq;

namespace com.mattymatty.AnalyticsAndStorage
{
    public class Database
    {
        private Socket _dbConnection;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool IsConnected()
        {
            return _dbConnection?.Connected ?? false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool StartConnection()
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Analytics.instance.GetConfigString("Analytics_server"));
            IPAddress ipAddr =
                ipHost.AddressList.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, Analytics.instance.GetConfigInt("Analytics_port"));
            Analytics.instance.Logger.Info("Analytics",
                "connecting to database on: " + localEndPoint.Address + "\n port: " + localEndPoint.Port);
            _dbConnection = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _dbConnection.Connect(localEndPoint);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool ExecuteNonQuery(string query)
        {
            try
            {
                string rec = SendAndReceive(query);

                if (rec.StartsWith("{"))
                {
                    JObject obj = JObject.Parse(rec);

                    if (obj.ContainsKey("error"))
                    {
                        Analytics.instance.Logger.Error("Analitics", "DB error: " + obj["error"]);
                        return false;
                    }

                    return true;
                }

                return true;
            }
            catch (SocketException)
            {
                Analytics.instance.Logger.Error("Analitics", "connection Closed");
                _dbConnection = null;
                return false;
            }
            catch (Exception ex)
            {
                Analytics.instance.Logger.Error("Analitics", ex.ToString());
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public JObject ExecuteQuery(string query)
        {
            try
            {
                var rec = SendAndReceive(query);

                if (rec.StartsWith("{"))
                {
                    JObject obj = JObject.Parse(rec);

                    if (obj.ContainsKey("error"))
                    {
                        Analytics.instance.Logger.Error("Analitics", "DB error: " + obj["error"]);
                        return obj;
                    }

                    return obj;
                }
                else
                {
                    JArray arr = JArray.Parse(rec);
                    JObject ret = new JObject();
                    ret.Add("result", arr);
                    return ret;
                }
            }
            catch (SocketException)
            {
                Analytics.instance.Logger.Error("Analitics", "connection Closed");
                _dbConnection = null;
                return null;
            }
            catch (Exception ex)
            {
                Analytics.instance.Logger.Error("Analitics", ex.ToString());
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private string SendAndReceive(string query)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(query);
            //db_connection.Send(BitConverter.GetBytes(bytes.Length));
            _dbConnection.Send(bytes);

            bytes = new byte[1024];

            int received = _dbConnection.Receive(bytes);

            string ret = Encoding.UTF8.GetString(bytes, 0, received);

            return ret;
        }
    }
}

  