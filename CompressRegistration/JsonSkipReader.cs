using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressRegistration
{
    class JsonSkipReader : JsonReader
    {
        JsonReader _innerReader;

        HashSet<string> _properties;

        string _propertyName;
        string _baseAddress;

        public JsonSkipReader(JsonReader innerReader)
        {
            _innerReader = innerReader;
            _innerReader.DateParseHandling = DateParseHandling.None;

            _properties = new HashSet<string>();
            
            // full strip

            _properties.Add("authors");
            _properties.Add("description");
            _properties.Add("iconUrl");
            _properties.Add("language");
            _properties.Add("licenseUrl");
            _properties.Add("minClientVersion");
            _properties.Add("projectUrl");
            _properties.Add("published");
            _properties.Add("requireLicenseAcceptance");
            _properties.Add("summary");
            _properties.Add("title");
            _properties.Add("tags");
            _properties.Add("commitId");
            _properties.Add("commitTimeStamp");
            _properties.Add("packageContent");
            _properties.Add("registration");
            _properties.Add("@type");
            _properties.Add("@context");

            // partial strip

            //_properties.Add("authors");
            //_properties.Add("iconUrl");
            //_properties.Add("language");
            //_properties.Add("minClientVersion");
            //_properties.Add("summary");
            //_properties.Add("title");
            //_properties.Add("commitId");
            //_properties.Add("commitTimeStamp");
            //_properties.Add("packageContent");
            //_properties.Add("registration");
            //_properties.Add("@type");
            //_properties.Add("@context");

            _propertyName = string.Empty;
            _baseAddress = "https://api.nuget.org/v3/";
        }

        public override bool Read()
        {
            if (!_innerReader.Read())
            {
                return false;
            }

            if (_innerReader.TokenType == JsonToken.PropertyName)
            {
                _propertyName = (string)_innerReader.Value;
            }

            if (_innerReader.TokenType == JsonToken.PropertyName && _properties.Contains((string)_innerReader.Value))
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
                if (_innerReader.TokenType == JsonToken.String && _propertyName == "@id")
                {
                    string address = (string)_innerReader.Value;
                    if (address.StartsWith(_baseAddress))
                    {
                        return address.Substring(_baseAddress.Length);
                    }
                }

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
