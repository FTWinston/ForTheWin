using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if SERVER
using FTW.Engine.Server;
#elif CLIENT
using FTW.Engine.Client;
#endif

namespace FTW.Engine.Shared
{
    public class Variable
    {
        private static SortedList<string, Variable> AllVariables = new SortedList<string, Variable>();

        public static IEnumerable<Variable> GetEnumerable() { return AllVariables.Values; }

        public static Variable Get(string name)
        {
            name = name.ToLower();
            if (AllVariables.ContainsKey(name))
                return AllVariables[name];
            return null;
        }

        public delegate bool StringCallback(Variable v, string newValue);
        public delegate bool NumericCallback(Variable v, float newValue);

        private void Validate(string name, VariableFlags flags)
        {
            Name = name;
            Flags = flags;

            if (Name.Contains(' '))
            {
                Console.Error.WriteLine("Variable name contains space(s), this is not allowed: '{0}'", Name);
                return;
            }

            if (AllVariables.ContainsKey(Name))
            {
                Console.Error.WriteLine("Duplicate variable name detected: {0}", Name);
                return;
            }

            const VariableFlags core = VariableFlags.Client | VariableFlags.Server | VariableFlags.ClientOnly | VariableFlags.ServerOnly;
            switch (flags & core)
            {
                case VariableFlags.None:
                    Console.Error.WriteLine("Variable {0} has no 'core' flag - must have one of Client, Server, ClientOnly and ServerOnly");
                    return;
                case VariableFlags.Client:
                case VariableFlags.Server:
                case VariableFlags.ClientOnly:
                case VariableFlags.ServerOnly:
                    break;
                default:
                    Console.Error.WriteLine("Variable {0} has multiple 'core' flags - must have only one of Client, Server, ClientOnly and ServerOnly");
                    return;
            }

#if SERVER
            if (HasFlags(VariableFlags.Client))
            {
                return;
            }
            else if (HasFlags(VariableFlags.ClientOnly))
            {
#if DEBUG
                Console.Error.WriteLine("Client-only variable {0} is defined on the server!", Name);
#endif
                return;
            }
#elif CLIENT
            if (HasFlags(VariableFlags.ServerOnly))
            {
#if DEBUG
                Console.Error.WriteLine("Server-only variable {0} is defined on the client!", Name);
#endif
                return;
            }
#endif

            AllVariables.Add(Name, this);
        }

        public Variable(string name, string defaultVal, VariableFlags flags, StringCallback callback)
        {
            Validate(name, flags);
            IsNumeric = false;
            DefaultValue = strVal = defaultVal;
            stringCallback = callback;
            numericCallback = null;
        }

        public Variable(string name, float defaultVal, VariableFlags flags, NumericCallback callback)
        {
            Validate(name, flags);
            IsNumeric = true;
            numericVal = defaultVal;
            DefaultValue = strVal = defaultVal.ToString();
            stringCallback = null;
            numericCallback = callback;
        }

        public Variable(string name, string defaultVal)
#if SERVER
            : this(name, defaultVal, VariableFlags.ServerOnly, null)
#elif CLIENT
            : this(name, defaultVal, VariableFlags.ClientOnly, null)
#endif
        {
        }

        public Variable(string name, float defaultVal)
#if SERVER
            : this(name, defaultVal, VariableFlags.ServerOnly, null)
#elif CLIENT
            : this(name, defaultVal, VariableFlags.ClientOnly, null)
#endif
        {
        }

        public Variable(string name, string defaultVal, VariableFlags flags)
            : this(name, defaultVal, flags, null)
        {
        }

        public Variable(string name, float defaultVal, VariableFlags flags)
            : this(name, defaultVal, flags, null)
        {
        }

        public string Name { get; private set; }
        public VariableFlags Flags { get; private set; }
        public bool IsNumeric { get; private set; }

        public bool HasFlags(VariableFlags flag) { return (Flags & flag) == flag; }
        public bool HasAnyFlag(VariableFlags flag) { return (Flags & flag) != VariableFlags.None; }

        private bool CanModify()
        {
#if CLIENT
            if (HasFlags(VariableFlags.Server))
                return false;
#endif

#if !DEBUG
            if (HasFlags(VariableFlags.Debug))
                return false;
#endif
            if (HasFlags(VariableFlags.Cheat) && !CheatsEnabled)
            {
                Console.WriteLine("Cannot modify {0}, when sv_cheats is not 1", Name);
                return false;
            }

            return true;
        }

        public bool IsDefault { get { return Value == DefaultValue; } }

        private string strVal;
        private float? numericVal;

        private StringCallback stringCallback;
        private NumericCallback numericCallback;

        public string DefaultValue { get; private set; }
        internal void ResetToDefault() { Value = DefaultValue; }
        public void SetDefaultValue(string value)
        {
            DefaultValue = value;
            ForceValue(value);
        }

        public string Value
        {
            get
            {
                return strVal;
            }
            set
            {
                if (!CanModify())
                    return;

                ChangeValue(value, null, false);
            }
        }

        public float NumericValue
        {
            get
            {
                if (numericVal.HasValue)
                    return numericVal.Value;
                return 0;
            }
            set
            {
                if (!CanModify())
                    return;

                ChangeValue(value.ToString(), value, false);
            }
        }

        private void ChangeValue(string newStrValue, float? flVal, bool forcedChange)
        {
            if (!IsNumeric)
            {
                if (stringCallback == null || stringCallback(this, newStrValue) || forcedChange)
                {
                    bool isChange = strVal != newStrValue;
                    strVal = newStrValue;
                    numericVal = null;

                    if (!forcedChange && isChange)
                        VariableChanged();
                }
                return;
            }

            if (flVal == null)
            {
                float numTmp;
                if (!float.TryParse(newStrValue, out numTmp))
                {
                    Console.Error.WriteLine("Value specified for {0} is not numeric ({1}), but this is a numeric variable", Name, strVal);
                    return;
                }
                flVal = numTmp;
            }

            if (numericCallback == null || numericCallback(this, flVal.Value) || forcedChange)
            {
                bool isChange = numericVal != flVal.Value;
                numericVal = flVal.Value;
                strVal = newStrValue;

                if (!forcedChange && isChange)
                    VariableChanged();
            }
        }

        internal void ForceValue(string value)
        {
            ChangeValue(value, null, true);
        }

        internal void ForceValue(float value)
        {
            ChangeValue(value.ToString(), value, true);
        }

        private void VariableChanged()
        {
#if SERVER
            if (HasFlags(VariableFlags.Server)) // don't send ServerOnly
#elif CLIENT
            if (HasFlags(VariableFlags.Client)) // don't send ClientOnly
#endif
            {
                Message m = new Message((byte)EngineMessage.VariableChange, RakNet.PacketPriority.MEDIUM_PRIORITY, RakNet.PacketReliability.RELIABLE_ORDERED, (int)OrderingChannel.Variables);
                m.Write(Name);
                m.Write(Value);

#if SERVER
                Client.SendToAll(m);
#elif CLIENT
                GameClient.Instance.SendMessage(m);
#endif
            }

#if SERVER
            if (Client.LocalClient != null)
                return; // if we have a local client, don't write the change to the console - that will happen client-side
#endif
            WriteChange(Name, Value);
        }

        internal static void WriteChange(string name, string val)
        {
            Console.WriteLine("{0} changed to {1}", name, val);
        }



        
        private static Variable cheats = new Variable("sv_cheats", 0, VariableFlags.Server, cheatsChanged);
        public static Variable Cheats { get { return cheats; } }

        public static bool CheatsEnabled
        {
            get
            {
                return cheats.NumericValue == 0;
            }
        }

        private static bool cheatsChanged(Variable v, float newVal)
        {
            if (newVal != 0f && newVal != 1f)
                return false;

            // if cheats were disabled, set all cheat variables to their default values
            foreach (Variable test in GetEnumerable())
                if (test.HasFlags(VariableFlags.Cheat) && !test.IsDefault)
                    test.ResetToDefault();

            return true;
        }

        // the rate at which the server (and client) simulate steps of the world
        private static Variable tickrate = new Variable("tickrate", 33, VariableFlags.Server, (v, val) =>
        {
            if (val >= 1 && val < 100)
            {
#if SERVER
                GameServer.Instance.TickInterval = (uint)(1000f/val);
#elif CLIENT
                GameClient.Instance.TickInterval = (uint)(1000f/val);
#endif
                return true;
            }
            return false;
        });

#if CLIENT
        // the number of snapshots per second that a client desires from the server
        private static Variable cl_snapshotrate = new Variable("cl_snapshotrate", 20, VariableFlags.Client, (v, val) =>
        {
            if (val >= sv_minsnapshotrate.NumericValue && val < sv_maxsnapshotrate.NumericValue)
            {
                //we don't need to use this on the client, do we?
                //GameClient.Instance.UpdateInterval = (uint)(1000f/val);
                return true;
            }
            return false;
        });
/*
        // the number of user commands per second that a client sends to the server
        private static Variable cl_cmdrate = new Variable("cl_cmdrate", 20, VariableFlags.Client, (v, val) =>
        {
            if (val >= sv_minsnapshotrate.NumericValue && val < sv_maxsnapshotrate.NumericValue)
            {
                GameClient.Instance.CommandInterval = (uint)(1000f / val);
                return true;
            }
            return false;
        });

        // the maximum amount of data that can be sent to this client, in kb/sec
        // hold off on sending snapshots if doing so would exceed this rate, over the last second
        private static Variable cl_datarate = new Variable("cl_datarate", 20, VariableFlags.Client, (v, val) =>
        {
            if (val >= sv_mindatarate.NumericValue && val < sv_maxdatarate.NumericValue)
            {
                // don't know how to use this, yet
                return true;
            }
            return false;
        });*/
#endif

        // snapshots/sec
        private static Variable sv_maxsnapshotrate = new Variable("sv_maxsnapshotrate", 100, VariableFlags.Server, (v, val) =>
        {
            if (val >= 1 && val < 100)
            {
                // any client with a cl_snapshotrate higher should have it reduced
                return true;
            }
            return false;
        });

        // snapshots/sec
        private static Variable sv_minsnapshotrate = new Variable("sv_minsnapshotrate", 1, VariableFlags.Server, (v, val) =>
        {
            if (val >= 1 && val < 100)
            {
                // any client with a cl_snapshotrate lower should have it reduced
                return true;
            }
            return false;
        });
/*
        // kb/sec
        private static Variable sv_maxdatarate = new Variable("sv_maxdatarate", 100, VariableFlags.Server, (v, val) =>
        {
            if (val >= 1 && val < 100)
            {
                // any client with a cl_datarate higher should have it reduced
                return true;
            }
            return false;
        });

        // kb/sec
        private static Variable sv_mindatarate = new Variable("sv_mindatarate", 1, VariableFlags.Server, (v, val) =>
        {
            if (val >= 1 && val < 100)
            {
                // any client with a cl_datarate lower should have it reduced
                return true;
            }
            return false;
        });
*/
/*
 * Game data is compressed using delta compression to reduce network load. That means the server doesn't send a full
 * world snapshot each time, but rather only changes (a delta snapshot) that happened since THE LAST ACKNOWLEDGED UPDATE.
 * With each packet sent between the client and server, acknowledge numbers are attached to keep track of their data flow.
 * Usually full (non-delta) snapshots are only sent when a game starts or a client suffers from heavy packet loss for a
 * couple of seconds. Clients can request a full snapshot manually with the cl_fullupdate command.

Should we do this? This would perhaps increase what we need to send, but might make "detecting loss" easier... ?

How would we decide that packet loss was so bad as to need a full update, though?
*/
    }
}
