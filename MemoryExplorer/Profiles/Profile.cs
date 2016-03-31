using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MemoryExplorer.Profiles
{
    public class Profile
    {
        private string _requestedImage;
        private string _cacheRoot;
        private Dictionary<string, JToken> _profileDictionary = null;
        private string _architecture = null;

        public string Architecture { get { return _architecture; } }

        public Profile(string sourceFile, string cacheRoot)
        {
            _requestedImage = sourceFile;
            if (!cacheRoot.EndsWith("\\"))
                cacheRoot += "\\";
            _cacheRoot = cacheRoot;
            // first check that the cache actually exists
            DirectoryInfo di = new DirectoryInfo(_cacheRoot);
            if (!di.Exists)
                throw new ArgumentException("Cache Directory Does Not Exist: " + _cacheRoot);
            bool onlineAvailable = false;
            Dictionary<string, JToken> githubInventory = null;
            try
            {
                // attempt to get the online inventory from rekall
                var rekallInventory = CheckInventory();
                // now try to get the inventory from github
                githubInventory = RetrieveFromGithub(@"v1.0\inventory.gz", true);
                onlineAvailable = true;
            }
            catch { }
            // now see if we have the file in the cache
            FileInfo fi = new FileInfo(_cacheRoot + _requestedImage);
            bool offlineAvailable = fi.Exists;
            // if offline isn't available but online is, download it and copy to the cache
            if (!offlineAvailable && onlineAvailable)
            {
                _profileDictionary = RetrieveFromGithub(_requestedImage, true);
                if (_profileDictionary == null)
                    onlineAvailable = false;
            }
            if (!offlineAvailable && !onlineAvailable)
            {
                MessageBox.Show("Couldn't Retrieve Profile " + _requestedImage + " from local cache or online.\n\nLocal cache is " + _cacheRoot + "\n\nYou might need to manually retrieve the profile using fetch_pdb.", "Profile Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new ArgumentException("Profile Could Not Be Located");
            }
                // if both online and offline are available, check to see if we have the latest version
            if (offlineAvailable && onlineAvailable)
            {
                var localInventory = GetLocalInventory();
                _profileDictionary = gzToDictionary(fi);
                try
                {
                    string t = GetTimestampFromInventory();
                    string t2 = GetTimestampFromProfile();
                    DateTime dt1 = Convert.ToDateTime(t);
                    DateTime dt2 = Convert.ToDateTime(t2);
                    // if the inventory timestamp indicates a new version
                    // go get the one from github
                    if (dt1 > dt2)
                        _profileDictionary = RetrieveFromGithub(_requestedImage, true);
                }
                catch { }
            }
            if (offlineAvailable && !onlineAvailable)
            {
                _profileDictionary = gzToDictionary(fi);
            }
            GetArchitectureFromProfile();
        }
        private void GetArchitectureFromProfile()
        {
            try
            {
                foreach (KeyValuePair<string, JToken> element in _profileDictionary)
                {
                    if (element.Value is JObject && element.Key == "$METADATA")
                    {
                        _architecture = (string)element.Value.SelectToken("arch");
                        // if the metadata doesn't help, use the size of the LIST_ENTRY structure
                        if (_architecture == null)
                        {
                            var s = GetStructureSize("_LIST_ENTRY");
                            if (s == 8)
                                _architecture = "I386";
                            else
                                _architecture = "AMD64"; // do the same as REKALL and default to AMD64 if you don't know any better
                        }
                        return;
                    }
                }
            }
            catch { }
        }
        public long GetStructureSize(string structure)
        {
            try
            {
                JArray node;
                foreach (KeyValuePair<string, JToken> element in _profileDictionary)
                {
                    if (element.Value is JObject && element.Key == "$STRUCTS")
                    {
                        node = (JArray)element.Value.SelectToken(structure);
                        if (node == null)
                            throw new System.ArgumentException("Structure " + structure + " Not Present");
                        return (long)((JValue)node[0]).Value;
                    }
                }
                return -1;
            }
            catch { return -1; }
        }
        private string GetTimestampFromInventory()
        {
            var localInventory = GetLocalInventory();
            int loc = _requestedImage.IndexOf("\\") + 1;
            string reqImage = (_requestedImage.Substring(loc).Replace("\\", "/")).Replace(".gz", "");

            foreach (KeyValuePair<string, JToken> element in localInventory)
            {
                if (element.Value is JObject && element.Key == "$INVENTORY")
                {
                    var node = element.Value.SelectToken(reqImage);
                    return (string)node["Timestamp"];
                }
            }
            throw new System.ArgumentException("Timestamp Not Present");

        }
        private string GetTimestampFromProfile()
        {
            foreach (KeyValuePair<string, JToken> element in _profileDictionary)
            {
                if (element.Value is JObject && element.Key == "$METADATA")
                {
                    return (string)element.Value.SelectToken("Timestamp");
                }
            }
            throw new System.ArgumentException("Timestamp Not Present");

        }
        private Dictionary<string, JToken> GetLocalInventory()
        {
            FileInfo fi = new FileInfo(_cacheRoot + @"v1.0\inventory.gz");
            if (!fi.Exists)
                return null;
            return gzToDictionary(fi);
        }
        private Dictionary<string, JToken> gzToDictionary(FileInfo fileInfo)
        {
            try
            {
                byte[] json = null;
                using (FileStream original = fileInfo.OpenRead())
                {
                    using (GZipStream gzStream = new GZipStream(original, CompressionMode.Decompress))
                    {
                        MemoryStream final = new MemoryStream();
                        gzStream.CopyTo(final);
                        long len = final.Length;
                        json = final.ToArray();
                    }
                }
                object parsedObject = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(json));
                var dict = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(parsedObject.ToString());
                return dict;
            }
            catch { return null; }
        }
        private Dictionary<string, JToken> RetrieveFromGithub(string filePath, bool cache = true)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    byte[] buffer = webClient.DownloadData("https://raw.githubusercontent.com/google/rekall-profiles/master/" + filePath);
                    // check to see if we want to cache the data
                    if (cache)
                    {
                        FileInfo fi = new FileInfo(_cacheRoot + filePath);
                        if (fi.Exists)
                            fi.Delete();
                        File.WriteAllBytes(_cacheRoot + filePath, buffer);
                    }
                    string json = Encoding.UTF8.GetString(Decompress(buffer));
                    object parsedObject = JsonConvert.DeserializeObject(json);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(parsedObject.ToString());
                    return dict;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private Dictionary<string, JToken> CheckInventory()
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    byte[] buffer = webClient.DownloadData("http://profiles.rekall-forensic.com/v1.0/inventory.gz");
                    string json = Encoding.UTF8.GetString(Decompress(buffer));
                    object parsedObject = JsonConvert.DeserializeObject(json);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(parsedObject.ToString());
                    return dict;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Couldn't Get Inventory Profile: " + ex.Message);
            }
        }
        private byte[] Decompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }
    }
}
