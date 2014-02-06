﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using System.Threading;

namespace FTW.Engine.Client
{
    public abstract class GameClient
    {
        public static GameClient Instance;
        internal ServerConnection Connection { get; private set; }
        public bool Connected { get { return Connection != null; } }
        public bool FullyConnected { get; internal set; }

        public Variable Name = new Variable("name", "unnamed", VariableFlags.Client, (v, val) =>
        {
            if (GameClient.Instance.FullyConnected)
            {
                Message m = new Message((byte)EngineMessage.ClientNameChange, RakNet.PacketPriority.MEDIUM_PRIORITY, RakNet.PacketReliability.RELIABLE_ORDERED, (int)OrderingChannel.Chat);
                m.Write(val);
                GameClient.Instance.Connection.Send(m);
                return false;
            }
            return true;
        });

        protected GameClient(Config settings)
        {
            Instance = this;
            FullyConnected = false;


            Variable var = new Variable("cl_interp", 200, VariableFlags.ClientOnly, (v, val) =>
            {
                if (val > 0 && val <= 1000)
                {
                    GameClient.Instance.LerpDelay = (uint)val;
                    return true;
                }
                return false;
            });

            SetupVariableDefaults();
            // read variables from config...


            LerpDelay = (uint)var.NumericValue;
        }

        protected abstract void SetupVariableDefaults();

        public void ConnectLocal()
        {
            if (Connection != null)
            {
                Console.Error.WriteLine("Cannot connect, already connected!");
                return;
            }
            Connection = new ListenServerConnection();
            Connection.Connect();
            Initialize();
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
            Initialize();
        }

        public void Disconnect()
        {
            Connection.Disconnect();

            if (Disconnected != null)
                Disconnected(this, EventArgs.Empty);

            Entity.AllEntities.Clear();
            Entity.NetworkedEntities.Clear();

            Connection = null;
            FullyConnected = false;
        }

        protected virtual void Initialize()
        {
            NetworkedEntity.InitializeTypes();

#if NET_INFO
            NetInfo = new NetworkInfo();
#endif

            dt = 100;
            nextFrameTime = RakNet.RakNet.GetTime();
            FrameTime = lastFrameTime = RakNet.RakNet.GetTime() - dt;
        }

        public event EventHandler Disconnected;

        protected internal uint TickInterval = 30;

        public uint FrameTime { get; private set; }
        public uint ServerTime { get; private set; }
        internal uint LerpDelay { get; private set; }
        private uint lastFrameTime, nextFrameTime, dt;

#if NET_INFO
        public NetworkInfo NetInfo { get; private set; }
#endif

        // this should really be a GameFrame type of affair, shouldn't it?
        public void Update()
        {
            FrameTime = RakNet.RakNet.GetTime();
            ServerTime = FrameTime - LerpDelay;

            Connection.RetrieveUpdates();
            Snapshot.CheckQueue();

            if ( nextFrameTime > FrameTime )
                return;

            /*if (Paused)
            {
                if (!pauseTime.HasValue)
                    pauseTime = DateTime.Now;
                Thread.Sleep(pauseTickMilliseconds);
                return;
            }
            else if (pauseTime.HasValue)
            {
                pauseTime = null;
                lastFrameTime = RakNet.RakNet.GetTime() - dt;
            }
            else*/
            {
                dt = FrameTime - lastFrameTime;
                lastFrameTime = FrameTime;
            }

#if NET_INFO
            if ( NetInfo.Enabled )
                NetInfo.Prune();
#endif
            PreUpdate();
            SendUpdate();
            GameFrame(dt / 1000.0); // convert milliseconds to seconds
            PostUpdate();

            nextFrameTime += TickInterval;
            lastFrameTime = FrameTime;
        }

        private void SendUpdate()
        {
            if (!FullyConnected)
                return;

            // don't actaully want this going out every frame. Should collect input and send at slightly more spread-out intervals.
            Message m = new Message((byte)EngineMessage.ClientUpdate, RakNet.PacketPriority.HIGH_PRIORITY, RakNet.PacketReliability.UNRELIABLE_SEQUENCED, 0);
            
            // write the "latest snapshot" info to the message

            WriteUpdate(m);
            SendMessage(m);
        }

        protected abstract void WriteUpdate(Message m);

        protected virtual void PreUpdate()
        {

        }

        protected virtual void PostUpdate()
        {

        }

        private void GameFrame(double dt)
        {
            int i; Entity e;
            for (i = 0; i < Entity.AllEntities.Count; i++)
            {
                e = Entity.AllEntities[i];
                if (!e.IsDeleted)
                    e.PreThink(dt);
            }
            for (i = 0; i < Entity.AllEntities.Count; i++)
            {
                e = Entity.AllEntities[i];
                if (!e.IsDeleted)
                    e.Simulate(dt);
            }
            for (i = 0; i < Entity.AllEntities.Count; i++)
            {
                e = Entity.AllEntities[i];
                if (!e.IsDeleted)
                    e.PostThink(dt);
            }
        }

        internal void HandleMessage(Message m)
        {
#if NET_INFO
            if (NetInfo.Enabled)
                NetInfo.Add(m, false); // this ignores raknet-interal packets
#endif
            if (!MessageReceived(m))
                Console.Error.WriteLine("Received an unrecognised message of Type " + m.Type);
        }

        protected internal virtual bool MessageReceived(Message m)
        {
            switch ((EngineMessage)m.Type)
            {
                case EngineMessage.InitialData:
                    {
                        byte[] hash = new byte[16];
                        m.Stream.Read(hash, 128);
                        if (!NetworkedEntity.CheckNetworkTableHash(hash))
                        {
                            Console.Error.WriteLine("Network table doesn't match server's");
                            Disconnect();
                            return true;
                        }

                        string name = m.ReadString();
                        Name.ForceValue(name);
                        Console.WriteLine("My name, corrected by server: " + name);

                        byte numOthers = m.ReadByte();

                        Console.WriteLine("There {0} {1} other {2} connected to this server{3}",
                            numOthers == 1 ? "is" : "are",
                            numOthers == 0 ? "no" : numOthers.ToString(),
                            numOthers == 1 ? "client" : "clients",
                            numOthers == 0 ? string.Empty : ":"
                            );

                        for (int i = 0; i < numOthers; i++)
                        {
                            string otherName = m.ReadString();
                            Console.WriteLine(" * " + otherName);
                        }

                        ushort num = m.ReadUShort();
                        for (int i = 0; i < num; i++)
                        {
                            Variable var = Variable.Get(m.ReadString());
                            string val = m.ReadString();
                            if ( var != null )
                            {
                                var.ForceValue(val);
                            }
                        }

                        GameClient.Instance.FullyConnected = true;
                        return true;
                    }
                case EngineMessage.ClientConnected:
                    {
                        string clientName = m.ReadString();

                        Console.WriteLine(clientName + " joined the game");
                        return true;
                    }
                case EngineMessage.ClientNameChange:
                    {
                        string newName = m.ReadString();

                        if ( m.ReadBool() )
                        {// it's me!
                            //this is a cheeky hack, to avoid having to add a new variable just to stop the callback sending it's message again
                            FullyConnected = false;
                            Name.ForceValue(newName);
                            FullyConnected = true;
                            Console.WriteLine("You changed your name to {0}", newName);
                        }
                        else
                        {// someone else
                            string oldName = m.ReadString();
                            Console.WriteLine("{0} changed name to {1}", oldName, newName);
                        }   
                        return true;
                    }
                case EngineMessage.Snapshot:
                    {
                        Snapshot.Read(m);
                        return true;
                    }
                case EngineMessage.VariableChange:
                    {
                        string name = m.ReadString();
                        string val = m.ReadString();
                        
                        Variable var = Variable.Get(name);
                        if (var != null)
                            var.ForceValue(val);
                        Variable.WriteChange(name, val);
                        return true;
                    }
                default:
                    return false;
            }
        }

        public void SendMessage(Message m)
        {
#if NET_INFO
            if (NetInfo.Enabled)
                NetInfo.Add(m, true); // this ignores raknet-interal packets
#endif
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

        static readonly char[] space = { ' ' };
        protected virtual bool ConsoleCommand(string firstWord, string theRest)
        {
            switch (firstWord)
            {
                case "disconnect":
                    Disconnect();
                    return true;
                case "get":
                    {
                        string name = theRest.Split(space)[0];
                        var vari = Variable.Get(name);
                        if (vari == null)
                        {
                            if (FullyConnected && Connection.IsLocal)
                                return false; // check server-only variables

                            Console.WriteLine("Variable not recognised: {0}", name);
                        }
                        else
                            Console.WriteLine("{0}: {1}", vari.Name, vari.Value);
                        return true;
                    }
                case "set":
                    {
                        string[] parts = theRest.Split(space, 2);
                        var vari = Variable.Get(parts[0]);
                        if (vari == null)
                        {
                            if (FullyConnected && Connection.IsLocal)
                                return false; // check server-only variables

                            Console.WriteLine("Variable not recognised: {0}", parts[0]);
                        }
                        else if (!vari.HasAnyFlag(VariableFlags.Client | VariableFlags.ClientOnly))
                        {
                            if (FullyConnected && Connection.IsLocal)
                                return false; // its a server variable ... set it on the server

                            Console.WriteLine("Cannot change server variable: {0}", vari.Name);
                        }
                        else
                            vari.Value = parts.Length > 1 ? parts[1] : string.Empty;
                        return true;
                    }
                default:
                    return false;
            }
        }
    }
}
