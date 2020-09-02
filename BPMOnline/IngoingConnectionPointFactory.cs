using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESB_ConnectionPoints.PluginsInterfaces;
namespace BPMOnline
{
    class IngoingConnectionPointFactory
    : IIngoingConnectionPointFactory
    {
        public const string BASE_URI = @"Базовый адрес сервера";
        public const string METHOD = @"Метод API";
        public const string USERNAME = @"Имя пользователя";
        public const string PASSWORD = @"Пароль";
        public const string CLASSID = @"Номер потока";
        public const string TYPE = @"Тип сообщения";
        public const string INTERVAL = @"Интервал опроса сервера";
        public const string TIMEOUT = @"Настройка Timeout";

        public IIngoingConnectionPoint Create(Dictionary<string, string> parameters,
        IServiceLocator serviceLocator)
        {
            string Login = parameters[USERNAME];
            string Password = parameters[PASSWORD];
            string Uri = parameters[BASE_URI];
            string ClassId = parameters[CLASSID];
            string Type = parameters[TYPE];
            string Interval = parameters[INTERVAL];
            string Method = parameters[METHOD];
            string Timeout = parameters[TIMEOUT];

            return new IngoingConnectionPoint(serviceLocator, Login, Password,
            Uri, ClassId, Type, Interval, Method, Timeout);

        }
    }
}