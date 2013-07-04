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
            if ( (flags & VariableFlags.Client) == VariableFlags.Client )
            {
                // add this to some client-variable list
                return;
            }
            else if ( (flags & VariableFlags.ClientOnly) == VariableFlags.ClientOnly )
            {
#if DEBUG
                Console.Error.WriteLine("Client-only variable {0} is defined on the server!", Name);
#endif
                return;
            }
#elif CLIENT
            if ((flags & VariableFlags.ServerOnly) == VariableFlags.ServerOnly)
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
            : this(name, defaultVal, VariableFlags.None, null)
        {
        }

        public Variable(string name, float defaultVal)
            : this(name, defaultVal, VariableFlags.None, null)
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

        private bool CanModify()
        {
#if CLIENT
            if ( (Flags & VariableFlags.Server) == VariableFlags.Server )
                return false;
#endif

#if !DEBUG
            if ( (Flags & VariableFlags.Debug) == VariableFlags.Debug )
                return false;
#endif
            if (!CheatsEnabled && (Flags & VariableFlags.Cheat) == VariableFlags.Cheat)
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
            Value = value;
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

                ChangeValue(value, null, true);
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

        private void ChangeValue(string strValue, float? flVal, bool forcedChange)
        {
            if (!IsNumeric)
            {
                if (stringCallback == null || stringCallback(this, strVal) || forcedChange)
                {
                    bool isChange = Value != strVal;
                    Value = strVal;
                    numericVal = null;

                    if (!forcedChange && isChange)
                        VariableChanged();
                }
                return;
            }

            if (flVal == null)
            {
                float numTmp;
                if (!float.TryParse(strValue, out numTmp))
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
                strVal = flVal.Value.ToString();

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
            if ((Flags & VariableFlags.Server) == VariableFlags.Server) // don't send ServerOnly
#elif CLIENT
            if ((Flags & VariableFlags.Client) == VariableFlags.Client) // don't send ClientOnly
#endif
            {
                Message m = new Message((byte)EngineMessage.VariableChange, RakNet.PacketPriority.MEDIUM_PRIORITY, RakNet.PacketReliability.RELIABLE_ORDERED);
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
                if ((test.Flags & VariableFlags.Cheat) == VariableFlags.Cheat && !test.IsDefault)
                    test.ResetToDefault(); // actually, we should report on this... no?

            return true;
        }
    }
}
