using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESB_ConnectionPoints.PluginsInterfaces;
using System.Net;
using System.IO;
using RestSharp;
using RestSharp.Authenticators;

namespace BPMOnline
{
    class IngoingConnectionPoint
    : IStandartIngoingConnectionPoint
    {
        private readonly ILogger _logger;
        private readonly IMessageFactory _messageFactory;
        private readonly string _uri;
        private readonly string _user;
        private readonly string _password;
        private readonly string _type;
        private readonly string _classId;
        private readonly int _interval;
        private readonly string _method;
        private readonly int _timeout;

        public IngoingConnectionPoint(IServiceLocator serviceLocator, string Login, string Password,
        string Uri, string Type, string ClassId, string Interval, string Method, string Timeout)
        {
            _logger = serviceLocator.GetLogger(GetType());
            _messageFactory = serviceLocator.GetMessageFactory();
            _uri = Uri;
            _user = Login;
            _password = Password;
            _type = Type;
            _classId = ClassId;
            _interval = Convert.ToInt32(Interval);
            _method = Method;
            _timeout = Convert.ToInt32(Timeout);
        }

        public void Cleanup()
        {
        }

        public void Dispose()
        {
        }

        public void Initialize()
        {
            if (string.IsNullOrEmpty(_uri))
            {
                throw new Exception("Не задан адрес сервера");
            }
            if (string.IsNullOrEmpty(_user))
            {
                _logger.Warning("Не указанно имя пользователя");
            }
            if (string.IsNullOrEmpty(_password))
            {
                _logger.Warning("Не указан пароль");
            }
            if (string.IsNullOrEmpty(_type))
            {
                _logger.Warning("Не указан тип отправляемого сообщения");
            }
            if (string.IsNullOrEmpty(_classId))
            {
                _logger.Warning("Не указан класс отправляемого сообщения");
            }
        }

        public void Run(IMessageHandler messageHandler, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                Tuple<HttpStatusCode, string> content = getCards();
                if (content.Item1 == HttpStatusCode.NoContent)
                {
                    ct.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));
                }
                else if (content.Item1 == HttpStatusCode.OK)
                {
                    messageHandler.HandleMessage(TryCreateMessage(content.Item2));
                    ct.WaitHandle.WaitOne(TimeSpan.FromMinutes(_interval));
                }
            }
        }

        private Message TryCreateMessage(string Body)
        {
            try
            {
                Message message = _messageFactory.CreateMessage(_type);
                message.Body = Encoding.UTF8.GetBytes(Body);
                message.ClassId = _classId;
                return message;
            }
            catch (Exception ex)
            {
                throw new Exception("Не удалось создать сообщение. " + ex.Message);
            }
        }

        private Tuple<HttpStatusCode, string> getCards()
        {
            //HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_uri);
            //req.Method = "GET";
            //req.ContentType = "application/json";
            //req.Accept = "application/json";
            //req.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("username:password"));
            //req.Credentials = new NetworkCredential("Supervisor", "Supervisor");

            //var content = string.Empty;

            //using (var response = (HttpWebResponse)req.GetResponse())
            //{
            // using (var stream = response.GetResponseStream())
            // {
            // using (var sr = new StreamReader(stream))
            // {
            // content = sr.ReadToEnd();
            // sr.Close();
            // return content;
            // }
            // }
            //}
            var client = new RestClient(_uri);
            var request = new RestRequest(_method, Method.GET);
            //request.AddHeader("Authorization", "Basic " + (Convert.ToBase64String(Encoding.Default.GetBytes("username:password"))));
            //request.Credentials = new NetworkCredential(_user, _password);
            client.Authenticator = new HttpBasicAuthenticator(_user, _password);
            request.Timeout = _timeout;
            IRestResponse response = client.Execute(request);

            return Tuple.Create(response.StatusCode, response.Content);

        }
    }
}