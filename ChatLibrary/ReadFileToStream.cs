using System.IO;

namespace ChatLibrary
{
    public class ReadFileToStream
    {
        private string fileName;
        private StreamString streamString;

        public ReadFileToStream(StreamString streamString, string fileName)
        {
            this.fileName = fileName;
            this.streamString = streamString;
        }

        public void Start()
        {
            string contents = File.ReadAllText(fileName);
            streamString.WriteString(contents);
        }
    }
}
