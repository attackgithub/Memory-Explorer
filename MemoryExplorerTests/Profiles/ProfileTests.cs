using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemoryExplorer.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json.Linq;
using MemoryExplorer.Model;

namespace MemoryExplorer.Profiles.Tests
{
    [TestClass()]
    public sealed class ProfileTests
    {
        private static Profile_Deprecated _profile;
        private static Dictionary<string, JToken> _profileDictionary;
        private static DataModel _model;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            _profile = new Profile_Deprecated(@"D67FECD43A49492C87EC845789255D641.gz", @"E:\Forensics\MxProfileCache", @"c:\temp", _model);
            _profileDictionary = _profile.ProfileDictionary;
        }
        [ClassCleanup()]
        public static void ClassCleanup()
        {
            
        }
        [TestMethod()]
        public void ProcessStructureAssemblyTest()
        {
            foreach (KeyValuePair<string, JToken> element in _profileDictionary)
            {
                if(element.Value is JObject && element.Key == "$STRUCTS")
                {
                    foreach(dynamic item in element.Value)
                    {
                        string name = item.Name;
                        if (name.Contains("unnamed"))
                            continue;
                        Assembly a = _profile.GetStructureAssembly(name);
                        Assert.IsNotNull(a);
                    }
                }
            }            
        }
    }
}