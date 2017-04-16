using MemoryExplorer.Profiles;
using MemoryExplorer.Worker;
using System;
using System.IO;

namespace MemoryExplorer.WorkerThreads
{
    public partial class ProcessingThread
    {
        private void LoadProfile(ref Job j)
        {
            try
            {
                string targetProfile = Path.Combine(Path.Combine(_model.ProfileCacheLocation, j.ActionMessage[0]), "LiveForensics.Symbols.dll");
                if (new FileInfo(targetProfile).Exists)
                {
                    _profile = new Profile(targetProfile, _dataProvider, _model);
                    if (_profile.Architecture == "I386")
                        _model.KiUserSharedData = 0xFFDF0000;
                    else if (_profile.Architecture == "AMD64")
                        _model.KiUserSharedData = 0xFFFFF78000000000;
                    _model.ActiveProfile = _profile;
                    j.Status = JobStatus.Complete;
                }
                else
                {
                    _model.ActiveProfile = null;
                    _profile = null;
                    j.Status = JobStatus.Failed;
                    j.ErrorMessage = "Failed to load requested profile: " + targetProfile;
                }
            }
            catch (Exception ex)
            {
                _model.ActiveProfile = null;
                _profile = null;
                j.Status = JobStatus.Failed;
                j.ErrorMessage = ex.Message;
            }
        }
    }
}
