using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESB_ConnectionPoints.PluginsInterfaces;

namespace BPMOnline
{
    class OutgoingConnectionPointFactory : IOutgoingConnectionPointFactory
    {
        //Параметры адаптера
        public const string BASE_URI    = @"Базовый адрес сервера";
        public const string USERNAME    = @"Имя пользователя";
        public const string PASSWORD    = @"Пароль";
        public const string OPTION      = @"Настройки потоков";
        public const string MAIN_METHOD = @"Основной метод";

        public IOutgoingConnectionPoint Create(Dictionary<string, string> parameters, 
            IServiceLocator serviceLocator)
        {
            if (!parameters.ContainsKey(BASE_URI))
            {
                throw new ArgumentException(string.Format("Не задан параметр <{0}>",
                    BASE_URI));
            }
            string baseUri      = parameters[BASE_URI];
            string username     = parameters[USERNAME];
            string password     = parameters[PASSWORD];
            string option       = parameters[OPTION];
            string mainMethod   = parameters[MAIN_METHOD];

            return new OutgoingConnectionPoint(serviceLocator, baseUri, username, password, option, mainMethod);

        }
    }
}
