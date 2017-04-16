using MemoryExplorer.Data;
using MemoryExplorer.Worker;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MemoryExplorer.WorkerThreads
{
    public partial class ProcessingThread
    {
        private void SetDataProvider(ref Job j)
        {
            string targetImage = _model.MemoryImageFilename;
            string ImageMd5 = GetMD5HashFromFile(targetImage);
            FileInfo fi = new FileInfo(targetImage);
            string cacheLocation = fi.Directory.FullName + "\\[" + fi.Name + "]" + ImageMd5;
            DirectoryInfo di = new DirectoryInfo(cacheLocation);
            if (!di.Exists)
                di.Create();
            _dataProvider = new ImageDataProvider(_model, cacheLocation);
            _model.DataProvider = _dataProvider;
            j.ActionMessage.Clear();
            j.Status = JobStatus.Complete;
            j.ActionMessage.Add(cacheLocation);
        }
        private string GetMD5HashFromFile(string filename)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var buffer = md5.ComputeHash(File.ReadAllBytes(filename));
                var sb = new StringBuilder();
                for (int i = 0; i < buffer.Length; i++)
                {
                    sb.Append(buffer[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
