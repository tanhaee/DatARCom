using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace DatAR.DataModels.Resources
{
    public interface IResource
    {
        string Id { get; set; }
    }
    
    // If data item is managed by C# type system, use this interface to build new implementations.
    // This is true for all Passables, which allow type-specific behaviour with Widgets.
    // For unmanaged types (e.g., LOD), the DynamicResource class is used.
    public class Resource : IEquatable<IResource>, IResource
    {
        [JsonProperty("@id")] public string Id { get; set; }

        public bool Equals(IResource other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IResource) obj);
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.GetHashCode() : 0);
        }

        // Source: https://www.c-sharpcorner.com/article/compute-sha256-hash-in-c-sharp/
        private static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        protected static string ComputeBlankNodeId(string data)
        {
            return "_:" + ComputeSha256Hash(data);
        }
    }
}