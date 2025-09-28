// ClimbSettingsDatumProperty.cs
using System;
using Unity.XR.CoreUtils.Datums;

namespace Meta.Interaction.Locomotion.Climbing
{
    [Serializable]
    public class ClimbSettingsDatumProperty : DatumProperty<ClimbSettings, ClimbSettingsDatum>
    {
        public ClimbSettingsDatumProperty(ClimbSettings value) : base(value)
        {
        }

        public ClimbSettingsDatumProperty(ClimbSettingsDatum datum) : base(datum)
        {
        }
    }
}