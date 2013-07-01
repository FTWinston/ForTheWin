using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                return false;

            return true;
        }

        public bool IsDefault { get { return Value == DefaultValue; } }

        private string strVal;
        private float? numericVal;

        private StringCallback stringCallback;
        private NumericCallback numericCallback;

        public string DefaultValue { get; internal set; }
        internal void SetDefault() { Value = DefaultValue; }

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

                string strTmp = value;
                float? numTmp;

                if (CheckChange(ref strTmp, out numTmp))
                {
                    bool isChange = strVal != null && strVal != strTmp;
                    strVal = strTmp;
                    numericVal = numTmp;
                    if (isChange)
                        VariableChanged();
                }
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

                float val = value;
                string newStrVal = val.ToString();

                if ((numericCallback == null || numericCallback(this, val) && (stringCallback == null || stringCallback(this, newStrVal))))
                {
                    bool isChange = strVal != null && strVal != newStrVal;
                    strVal = newStrVal;
                    numericVal = val;
                    if (isChange)
                        VariableChanged();
                }
            }
        }

        private bool CheckChange(ref string strVal, out float? numValue)
        {
            numValue = null;
            if (!IsNumeric)
                return stringCallback == null || stringCallback(this, strVal);

            float numTmp;
            if (!float.TryParse(strVal, out numTmp))
            {
                Console.Error.WriteLine("Value specified for {0} is not numeric ({1}), but this is a numeric variable", Name, strVal);
                return false;
            }

            numValue = numTmp;

            // it might still have a string callback, so consider that as well as the numeric one
            return (numericCallback == null || numericCallback(this, numTmp)) && (stringCallback == null || stringCallback(this, strVal));
        }

        private void VariableChanged()
        {
            // ... need to be able to notify others of this
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
                    test.SetDefault(); // actually, we should report on this... no?

            return true;
        }
    }
}
