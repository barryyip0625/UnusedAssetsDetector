using UnityEngine;
using System.Collections.Generic;   

namespace BYUtils.AssetsManagement
{
    public class WhitelistObject : ScriptableObject
    {
        public List<string> folders;
        public List<string> files;
    }
}
