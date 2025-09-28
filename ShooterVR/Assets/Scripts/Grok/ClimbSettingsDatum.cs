// ClimbSettingsDatum.cs
using UnityEngine;
using Unity.XR.CoreUtils.Datums;  // Asume tienes XR Core Utils instalado

namespace Meta.Interaction.Locomotion.Climbing
{
    [CreateAssetMenu(fileName = "ClimbSettings", menuName = "XR/Locomotion/Climb Settings")]
    public class ClimbSettingsDatum : Datum<ClimbSettings>
    {
    }
}