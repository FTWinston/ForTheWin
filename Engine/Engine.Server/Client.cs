using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNet;

namespace FTW.Engine.Server
{
    public abstract class Client
    {
        protected Client() { }

        public string Name { get; set; }
        public abstract bool IsLocal { get; }

        internal static SortedList<ulong, Client> AllClients = new SortedList<ulong, Client>();
        public IList<Client> GetAll() { return AllClients.Values; }

        public static List<Client> GetAllExcept(params Client[] exclude)
        {
            List<Client> list = new List<Client>();
            foreach (Client c in AllClients.Values)
            {
                bool excluded = false;
                foreach (Client ex in exclude)
                    if (c == ex)
                    {
                        excluded = true;
                        break;
                    }
                if (!excluded)
                    list.Add(c);
            }
            return list;
        }

        public static List<Client> GetAllExcept(IEnumerator<Client> exclude)
        {
            List<Client> list = new List<Client>();
            foreach (Client c in AllClients.Values)
            {
                exclude.Reset();
                bool excluded = false;
                do
                {
                    if (c == exclude.Current)
                    {
                        excluded = true;
                        break;
                    }
                } while (exclude.MoveNext());

                if (!excluded)
                    list.Add(c);
            }
            return list;
        }

        public static List<Client> GetAllExceptLocal()
        {
            List<Client> cl = new List<Client>();
            foreach (Client c in AllClients.Values)
            {
                if (c.IsLocal)
                    continue;
                cl.Add(c);
            }
            return cl;
        }

        public static Client LocalClient { get; protected set; }

        public static Client GetByName(string name)
        {
            foreach (Client c in AllClients.Values)
                if (c.Name == name)
                    return c;
            return null;
        }

        internal static Client GetByUniqueID(RakNetGUID uniqueID)
        {
            if (AllClients.ContainsKey(uniqueID.g))
                return AllClients[uniqueID.g];
            return null;
        }

        protected static string GetUniqueName(Client c, string desiredName)
        {
            desiredName = GameServer.Instance.ValidatePlayerName(desiredName);

            string newName = desiredName;
            int i = 1;

            Client matchingName = Client.GetByName(newName);
            while (matchingName != null && matchingName != c)
            {
                newName = string.Format("{0} ({1})", desiredName, i);
                i++;

                matchingName = Client.GetByName(newName);
            }
            return newName;
        }
    }

    internal class LocalClient : Client
    {
        private LocalClient() { }
        public override bool IsLocal { get { return true; } }

        public static Client Create(string desiredName)
        {
            LocalClient c = new LocalClient();
            string name = GetUniqueName(c, desiredName);
            c.Name = name;

            AllClients.Add(RakNet.RakNet.UNASSIGNED_RAKNET_GUID.g, c);
            LocalClient = c;
            return c;
        }
    }

    internal class RemoteClient : Client
    {
        private RemoteClient() { }
        public override bool IsLocal { get { return false; } }

        public static Client Create(string desiredName, RakNetGUID uniqueID)
        {
            RemoteClient c = new RemoteClient();
            string name = GetUniqueName(c, desiredName);
            c.Name = name;

            AllClients.Add(uniqueID.g, c);
            return c;
        }
    }
}
