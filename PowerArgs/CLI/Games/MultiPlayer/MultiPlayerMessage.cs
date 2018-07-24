using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PowerArgs.Games
{
    public class MultiPlayerMessage
    {
        private Dictionary<string, string> _data = new Dictionary<string, string>();
        public string RawContents { get; private set; }
        public string EventId { get; private set; }
        public IReadOnlyDictionary<string, string> Data => new ReadOnlyDictionary<string, string>(_data); 
        public string RecipientId { get; private set; }
        public string SenderId { get; private set; }

        public static MultiPlayerMessage Parse(string rawMessageContent)
        {
            var ret = new MultiPlayerMessage() { RawContents = rawMessageContent };
            var lines = rawMessageContent.Split('\n');
            ret.EventId = Base64Decode(lines[0]);

 
            for (var i = 1; i < lines.Length; i++)
            {
                var split = lines[i].Split(':');
                var key = Base64Decode(split[0]);
                var value = Base64Decode(split[1]);

                if (key == nameof(RecipientId))
                {
                    ret.RecipientId = value;
                }
                else if (key == nameof(SenderId))
                {
                    ret.SenderId = value;
                }
                else
                {
                    ret._data.Add(key, value);
                }
            }

            return ret;
        }

        public static MultiPlayerMessage Create(string sender, string recipient, string eventId, Dictionary<string, string> data = null)
        {

            data = data ?? new Dictionary<string, string>();
            data.Add(nameof(RecipientId), recipient);
            data.Add(nameof(SenderId), sender);

            var messageContents = Base64Encode(eventId);

            foreach (var property in data)
            {
                messageContents += "\n" + Base64Encode(property.Key) + ":" + Base64Encode(property.Value);
            }
            
            data.Remove(nameof(RecipientId));
            data.Remove(nameof(SenderId));

            return new MultiPlayerMessage()
            {
                SenderId = sender,
                RecipientId = recipient,
                RawContents = messageContents,
                EventId = eventId,
                _data = data,
            };
        }

        public void AddProperty(string key, string value)
        {
            _data.Add(key, value);
            RawContents += "\n" + Base64Encode(key) + ":" + Base64Encode(value);
        }

        private static string Base64Encode(string plainText)
        {
            if (plainText == null) return "$null";
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private static string Base64Decode(string base64EncodedData)
        {
            if (base64EncodedData == "$null")
            {
                return null;
            }
            else
            {
                var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
        }
    }
}
