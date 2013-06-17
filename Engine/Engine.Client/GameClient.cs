﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;

namespace FTW.Engine.Client
{
    public abstract class GameClient
    {
        public static GameClient Instance;
        internal ServerConnection Connection { get; private set; }
        public bool Connected { get { return Connection != null; } }
        public bool FullyConnected { get; internal set; }

        private string name;
        public string Name 
        {
            get { return name; }
            set
            {
                if (name == value)
                    return;

                if (FullyConnected)
                {
                    Message m = new Message((byte)EngineMessage.ClientNameChange, RakNet.PacketPriority.HIGH_PRIORITY, RakNet.PacketReliability.RELIABLE_SEQUENCED);
                    m.Write(value);
                    SendMessage(m);
                }
                else if (Connected)
                {
                    Console.Error.WriteLine("Can't change name while connecting to server!");
                    return;
                }

                name = value;
            }
        }

        private const string defaultClientName = "unnamed";
        protected GameClient(Config settings)
        {
            Instance = this;
            FullyConnected = false;

            Name = settings.FindValueOrDefault("name", defaultClientName);
        }

        public void ConnectLocal()
        {
            if (Connection != null)
            {
                Console.Error.WriteLine("Cannot connect, already connected!");
                return;
            }
            Connection = new ListenServerConnection();
            Connection.Connect();
        }

        public void ConnectRemote(string hostname, ushort port)
        {
            if (Connection != null)
            {
                Console.Error.WriteLine("Cannot connect, already connected!");
                return;
            }
            Connection = new RemoteClientConnection(hostname, port);
            Connection.Connect();
        }

        public void Disconnect()
        {
            Connection.Disconnect();

            if (Disconnected != null)
                Disconnected(this, EventArgs.Empty);

            Connection = null;
            FullyConnected = false;
        }

        public event EventHandler Disconnected;

        public void Update()
        {
            Connection.RetrieveUpdates();

        }

        internal void HandleMessage(Message m)
        {
            if (!MessageReceived(m))
                Console.Error.WriteLine("Received an unrecognised message of Type " + m.Type);
        }

        protected internal virtual bool MessageReceived(Message m)
        {
            switch ((EngineMessage)m.Type)
            {
                case EngineMessage.ClientConnected:
                    {
                        string clientName = m.ReadString();

                        Console.WriteLine(clientName + " joined the game");
                        return true;
                    }
                case EngineMessage.PlayerList:
                    {
                        name = m.ReadString(); // change name rather than Name, so as to automatically accept the change without re-sending.
                        Console.WriteLine("My name, corrected by server: " + Name);

                        byte numOthers = m.ReadByte();

                        Console.WriteLine(string.Format("There {0} {1} other {2} connected to this server{3}",
                            numOthers == 1 ? "is" : "are",
                            numOthers == 0 ? "no" : numOthers.ToString(),
                            numOthers == 1 ? "client" : "clients",
                            numOthers == 0 ? string.Empty : ":"
                            ));

                        for (int i = 0; i < numOthers; i++)
                        {
                            string otherName = m.ReadString();
                            Console.WriteLine(" * " + otherName);
                        }

                        GameClient.Instance.FullyConnected = true;
                        return true;
                    }
                case EngineMessage.Snapshot:
                    {
                        // add this message to a list sorted by timestamp, assuming it isn't too old
                        
                        // As we're only using timestamps, how should we determine (when trying to apply it) if we're MISSING a snapshot, or not?
                        // Rather than have a frame number in the snapshot, we can compare the time difference to the server frame interval variable.
                        // That would struggle when the variable changes ... or would it? If the variable change wasn't applied until the relevant snapshot came in, we might get away with it.
                        return true;
                    }
                default:
                    return false;
            }
        }

        public void SendMessage(Message m)
        {
            Connection.Send(m);
        }

        static readonly char[] cmdSplit = { ' ', '	' };
        public void HandleCommand(string cmd)
        {
            Console.WriteLine(cmd);

            // split the command into words, for ease of processing
            string[] words = cmd.Split(cmdSplit, 2, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return;

            // for the moment, trying the command on the client before the server (for local listen servers only). May want to swap that.
            if (!ConsoleCommand(words[0], words.Length > 1 ? words[1] : null))
            {
                // if this is a listen server, try running the command on the server. That'll report an error if it doesn't recognise the command either.
                if (FullyConnected && Connection.IsLocal)
                    (Connection as ListenServerConnection).ConsoleCommand(cmd);
                else
                    Console.Error.WriteLine("Command not recognised: " + words[0]);
            }
        }

        protected virtual bool ConsoleCommand(string firstWord, string theRest)
        {
            switch (firstWord)
            {

                default:
                    return false;
            }
        }
    }
}
