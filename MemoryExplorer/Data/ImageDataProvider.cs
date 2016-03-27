using MemoryExplorer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryExplorer.Data
{
    public class ImageDataProvider : DataProviderBase
    {
        private string _imageFilename;
        private ulong _imageLength = 0;

        public ImageDataProvider(DataModel data) : base(data)
        {
            _imageFilename = "";
        }
        public override Dictionary<string, object> GetInformation()
        {
            Dictionary<string, object> info = new Dictionary<string, object>();
            info.Add("dtb", 0x01aa00);
            info.Add("buildNumber", 4444);
            info.Add("kernelBase", 0xf80012542332);
            return info;
        }
        public override byte[] ReadMemoryPage(ulong address)
        {
            if (_data.MemoryImageFilename == "")
                throw new ArgumentException("Memory Image Name Isn't Set");

            // check to see if we are looking at a new image file
            if (_data.MemoryImageFilename != _imageFilename)
            {
                _imageFilename = _data.MemoryImageFilename;
                FileInfo fiCheck = new FileInfo(_imageFilename);
                if (!fiCheck.Exists)
                {
                    _imageFilename = "";
                    _imageLength = 0;
                    throw new ArgumentException("Memory Image Doesn't Exist: " + _imageFilename);
                }
                _imageLength = (ulong)fiCheck.Length;
            }
            if(address > _imageLength - 4096)
                throw new ArgumentException("Address Beyond End Of File");

            byte[] buffer = new byte[4096];
            try
            {
                using (FileStream fs = new FileStream(_imageFilename, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    fs.Seek((long)address, SeekOrigin.Begin);
                    buffer = br.ReadBytes(4096);
                }
                return buffer;
            }
            catch
            {
                return null;
            }
        }

    }
}
