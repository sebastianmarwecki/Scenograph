using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.VirtualSpace.Unity_Specific;
using RootMotion.FinalIK;
using UnityEngine;

namespace Assets.HumanModel
{
    public class CheckAvailability : MonoBehaviour
    {

        public List<GameObject> NeedToBeActive;
        public VRIK ToActivateVrIk;
        public IkTargetAdapt ToActivateIkTargetAdapt;
    
        void Update ()
        {
            var activate = NeedToBeActive.All(n => n.activeSelf);
            ToActivateIkTargetAdapt.enabled = activate;
            ToActivateVrIk.enabled = activate;
        }
    }
}
