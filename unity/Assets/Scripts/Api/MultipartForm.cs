using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ColonQuest.Api
{
    /// <summary>
    /// A multipart/form-data body built by hand, so it travels through any <see cref="IHttpTransport"/>
    /// as a raw body with its content type. It has no Unity dependency and is tested in EditMode.
    /// </summary>
    public sealed class MultipartForm
    {
        private const string LineBreak = "\r\n";

        private readonly List<Part> parts = new List<Part>();

        public MultipartForm(string boundary = null)
        {
            Boundary = string.IsNullOrEmpty(boundary) ? "----ColonQuest" + Guid.NewGuid().ToString("N") : boundary;
        }

        public string Boundary { get; }

        /// <summary>The value for the Content-Type header, boundary included.</summary>
        public string ContentType => "multipart/form-data; boundary=" + Boundary;

        public MultipartForm AddField(string name, string value)
        {
            parts.Add(new Part { Name = name, Data = Encoding.UTF8.GetBytes(value ?? string.Empty) });
            return this;
        }

        public MultipartForm AddFile(string name, string fileName, string contentType, byte[] data)
        {
            parts.Add(new Part { Name = name, FileName = fileName, ContentType = contentType, Data = data ?? new byte[0] });
            return this;
        }

        public byte[] ToBytes()
        {
            using (var body = new MemoryStream())
            {
                foreach (var part in parts)
                {
                    var header = new StringBuilder();
                    header.Append("--").Append(Boundary).Append(LineBreak);
                    header.Append("Content-Disposition: form-data; name=\"").Append(Quote(part.Name)).Append('"');
                    if (part.FileName != null)
                    {
                        header.Append("; filename=\"").Append(Quote(part.FileName)).Append('"');
                    }

                    header.Append(LineBreak);
                    if (part.ContentType != null)
                    {
                        header.Append("Content-Type: ").Append(part.ContentType).Append(LineBreak);
                    }

                    header.Append(LineBreak);
                    Write(body, header.ToString());
                    body.Write(part.Data, 0, part.Data.Length);
                    Write(body, LineBreak);
                }

                Write(body, "--" + Boundary + "--" + LineBreak);
                return body.ToArray();
            }
        }

        private static void Write(Stream stream, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>Names never carry quotes or line breaks, so a header cannot be split.</summary>
        private static string Quote(string value)
        {
            return (value ?? string.Empty).Replace("\"", "%22").Replace("\r", string.Empty).Replace("\n", string.Empty);
        }

        private sealed class Part
        {
            public string Name;
            public string FileName;
            public string ContentType;
            public byte[] Data;
        }
    }
}
