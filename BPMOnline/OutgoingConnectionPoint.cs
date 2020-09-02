using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESB_ConnectionPoints.PluginsInterfaces;
using Newtonsoft.Json;
using System.Net;
using System.Reflection;
using System.IO;
using RestSharp;
using System.Runtime.Caching;
using RestSharp.Authenticators;
using System.Threading;

namespace BPMOnline
{
    class OutgoingConnectionPoint
        : ISimpleOutgoingConnectionPoint
    {

        private readonly ILogger _logger;
        private readonly string _login;
        private readonly string _password;
        private readonly string _baseUri;
        private readonly string _mainMethod;
        private readonly string _option;
        public bool NeedPost { get; set; }
        public Method Method { get; set; }
        public string Resource { get; set; }
        protected MemoryCache cache = new MemoryCache("CachingProvider");
        public Task[] tasks = new Task[4];

        static readonly object padlock = new object();
        public OutgoingConnectionPoint(IServiceLocator serviceLocator, string baseUri
            , string username, string password, string option, string mainMethod)
        {
            _baseUri = baseUri;
            _login = username;
            _password = password;
            _mainMethod = mainMethod;
            _option = option;
            _logger = serviceLocator.GetLogger(GetType());
        }
        public bool IsReady()
        {
            return true;
        }
        public bool HandleMessage(Message message, IMessageReplyHandler replyHandler)
        {
            try
            {
                var cacheData = GetItem(message.ClassId, false);
                //Исключение на неизвестный для адаптера ClassId
                if (cacheData == null)
                {
                    throw new Exception("В кэше не сохранены настройки для ClassId " + message.ClassId + ".\n\tПроверьте настройки адаптера");
                }
                //Получение полей из object кэша памяти
                NeedPost = (bool)cacheData.GetType().GetField("needPost").GetValue(cacheData);
                Method = (Method)cacheData.GetType().GetField("method").GetValue(cacheData);
                Resource = (string)cacheData.GetType().GetField("api").GetValue(cacheData);
                //Удаление сообщений на приемнике
                if (message.Type == "DLT")
                {
                    IRestResponse response = executeRequest(message.ClassId, Encoding.UTF8.GetString(message.Body), Method.DELETE, Resource);
                    loggerInfo(response.StatusCode.ToString(), response.Content);
                    return true;
                }
                //Если true , происходит POST , а потом метод из настроек.
                if (NeedPost)
                {
                    IRestResponse response = executeRequest(message.ClassId, Encoding.UTF8.GetString(message.Body), Method.POST, Resource);
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        IRestResponse resp = executeRequest(message.ClassId, Encoding.UTF8.GetString(message.Body), Method, Resource);
                        if (resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.Created)
                        {
                            loggerInfo(resp.StatusCode.ToString(), resp.Content);
                        }
                        else
                        {
                            getThrow(message.Id, response.StatusCode, response.Content);
                        }
                    }
                    else if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                    {
                        loggerInfo(response.StatusCode.ToString(), response.Content);
                    }
                }
                else //Иначе сразу переходим к методу в настройках
                {
                    IRestResponse resp = executeRequest(message.ClassId, Encoding.UTF8.GetString(message.Body), Method, Resource);
                    if (resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.Created)
                    {
                        loggerInfo(resp.StatusCode.ToString(), resp.Content);
                    }
                    else
                    {
                        getThrow(message.Id, resp.StatusCode, resp.Content);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return true;
        }

        public void Initialize()
        {
            _logger.Debug("Начало чтения настроек в кэш");
            TryCreateObjectToCache();
        }
        public void Cleanup()
        {

        }

        public void Dispose()
        {
            cache.Dispose();
        }

        //REST client
        public IRestResponse executeRequest(string ClassId, string Body, Method method, string appi)
        {
                RestClient client = new RestClient(_baseUri);
                RestRequest request = new RestRequest(appi, method);
                client.Authenticator = new HttpBasicAuthenticator(_login, _password);
                request.RequestFormat = DataFormat.Json;
                request.Timeout = 600000;
                request.AddParameter("application/json", Body, ParameterType.RequestBody);
                IRestResponse respone = client.Execute(request);
                return respone;
        }
        //
        public void getMethod(string method)
        {
            //Попробовать Switch
            if (method == "POST")
            {
                Method = Method.POST;
            }
            if (method == "PUT")
            {
                Method = Method.PUT;
            }
            if (method == "PATCH")
            {
                Method = Method.PATCH;
            }
        }

        public void getThrow(Guid id, HttpStatusCode respStatus, string respContent)
        {
            throw new Exception("Произошла ошибка при выполнении запроса. Id сообщения : " + id + Environment.NewLine + "Статус : " + respStatus + " Метод : " + Method + " ссылка на API : " + _baseUri + Resource + Environment.NewLine + "Описание :  " + respContent);
        }

        public void loggerInfo(string statusCode, string content)
        {
            _logger.Info("[" + statusCode + "] " + " Метод [" + Method + "] " + content);
        }

        public void TryCreateObjectToCache()
        {
            dynamic root = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(_option);
            root = root["ClassId"];

            foreach (var element in root)
            {
                foreach (var value in element)
                {
                    EsbMessage _message = new EsbMessage { api = value["Appi"], method = value["Method"], needPost = value["NeedPost"] };
                    AddItem(element.Name, _message);
                }
            }
            _logger.Debug("Чтение настроек в кэш завершено");
        }

        public virtual void AddItem(string key, object value)
        {
            lock (padlock)
            {
                cache.Add(key, value, DateTimeOffset.MaxValue);
            }
        }

        protected virtual object GetItem(string key, bool remove)
        {
            lock (padlock)
            {
                var res = cache[key];

                if (res != null)
                {
                    if (remove == true)
                    {
                        cache.Remove(key);
                    }
                }
                return res;
            }

        }

    }
    class EsbMessage
    {
        public Method method;
        public string api;
        public bool needPost;
    }
}