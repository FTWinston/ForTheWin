using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel;
using System.IO;

namespace FTW.Engine.Shared
{
    public class Config
    {
        public static Config ReadFile(string path)
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine("Config file not found: " + path);
                return null;
            }

            var yaml = new YamlStream();
            using ( StreamReader reader = File.OpenText(path) )
            {
                try
                {
                    yaml.Load(reader);
                }
                catch (Exception)
                {
                    Console.Error.WriteLine("Error parsing config file: " + path);
                    return null;
                }
            }

            try
            {
                YamlMappingNode mapping = yaml.Documents[0].RootNode as YamlMappingNode;
                return AddToConfig(mapping, null, string.Empty);
            }
            catch (Exception)
            {
                Console.Error.WriteLine("Error loading config from file: " + path);
                return null;
            }
        }

        private static Config AddToConfig(YamlNode yamlNode, Config parent, string nodeName)
        {
            Config node = new Config(nodeName);
            if (yamlNode is YamlMappingNode)
            {
                YamlMappingNode mapping = yamlNode as YamlMappingNode;
                node.Children = new List<Config>();
                foreach (var child in mapping.Children)
#if DEBUG
                    if (child.Key is YamlScalarNode)
#endif
                        AddToConfig(child.Value, node, (child.Key as YamlScalarNode).Value);
#if DEBUG
                    else // going on the assumption that a Key is *always* a string. Can't wrap my head around how it wouldn't be.
                        throw new InvalidCastException("YamlMappingNode's key is NOT a YamlScalarNode: " + child.Key.GetType().FullName);
#endif
            }
            else if (yamlNode is YamlSequenceNode)
            {
                YamlSequenceNode sequence = yamlNode as YamlSequenceNode;
                node.Children = new List<Config>();
                foreach( var child in sequence.Children )
                    AddToConfig(child, node, child.Tag);
            }
            else if (yamlNode is YamlScalarNode)
            {
                YamlScalarNode scalar = yamlNode as YamlScalarNode;
                node.Value = scalar.Value;
            }
            else
                throw new InvalidCastException("Unrecognised YamlNode type: " + yamlNode.GetType().FullName);
            
            if ( parent != null )
                parent.Children.Add(node);
            return node;
        }


        internal Config(string name) { Name = name; Value = null; Children = null; }
        public string Name { get; internal set; }
        public string Value { get; internal set; }
        public List<Config> Children { get; internal set; }

        public bool HasValue { get { return Value != null; } }
        public bool HasChildren { get { return Children != null && Children.Count > 0;} }

        public Config Find(string name)
        {
            if (!HasChildren)
                return null;
            
            foreach (Config child in Children)
                if (child.Name == name)
                    return child;

            return null;
        }

        public string FindValueOrDefault(string name, string defaultValue)
        {
            Config node = Find(name);
            if (node != null && node.HasValue)
                return node.Value;
            return defaultValue;
        }
    }
}
