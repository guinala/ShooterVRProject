using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using UnityEngine;

public class InteractorPoses : MonoBehaviour
{
    // Mapea identificador del interactor al Pose actual
    private Dictionary<int, Pose> _poses = new Dictionary<int, Pose>();

    public void UpdatePose(int identifier, Pose pose)
    {
        _poses[identifier] = pose;
    }

    public bool TryGetPose(int identifier, out Pose pose)
    {
        return _poses.TryGetValue(identifier, out pose);
    }
}