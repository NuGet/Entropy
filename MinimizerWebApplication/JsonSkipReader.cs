using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompressRegistration
{
    class JsonSkipReader : JsonReader
    {
        JsonReader _innerReader;
        List<Regex> _properties;

        public JsonSkipReader(JsonReader innerReader, IEnumerable<string> jsonPathToSkip)
        {
            _innerReader = innerReader;
            _innerReader.DateParseHandling = DateParseHandling.None;

            _properties = jsonPathToSkip.Select(path => MakeRegex(path)).ToList();
        }

        static Regex MakeRegex(string jsonPath)
        {
            string pattern = jsonPath.Replace("[*]", @"\[\d+\]").Replace(".", @"\.");
            Regex exp = new Regex("^" + pattern);
            return exp;
        }

        bool TestPath(string jsonPath)
        {
            foreach (var property in _properties)
            {
                if (property.IsMatch(jsonPath))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool Read()
        {
            if (!_innerReader.Read())
            {
                return false;
            }
            
            if (_innerReader.TokenType == JsonToken.PropertyName && TestPath(_innerReader.Path))
            {
                _innerReader.Skip();

                return Read();
            }

            return true;
        }

        public override object Value
        {
            get
            {
                return _innerReader.Value;
            }
        }

        public override JsonToken TokenType
        {
            get
            {
                return _innerReader.TokenType;
            }
        }

        public override string Path
        {
            get
            {
                return _innerReader.Path;
            }
        }

        public override int Depth
        {
            get
            {
                return _innerReader.Depth;
            }
        }

        public override Type ValueType
        {
            get
            {
                return _innerReader.ValueType;
            }
        }
    }
}
