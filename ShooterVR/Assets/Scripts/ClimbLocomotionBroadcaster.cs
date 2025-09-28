using System;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.Locomotion;   // Asegúrate de que el namespace coincida con tu versión
using Oculus.Interaction; 

public class ClimbLocomotionBroadcaster : MonoBehaviour, ILocomotionEventBroadcaster
{
    class GrabState
    {
        public int identifier;
        public Transform climbTransform;
        public Vector3 anchorLocal;
        public Vector3 allowedAxes;
    }

    private readonly List<GrabState> _grabs = new List<GrabState>();

    public event Action<LocomotionEvent> WhenLocomotionPerformed;
    private InteractorPoses _poseStore;

    private void Awake()
    {
        // localizar el pose store
        _poseStore = FindObjectOfType<InteractorPoses>();
        if (_poseStore == null)
        {
            Debug.LogWarning("ClimbLocomotionBroadcaster_Meta: no se encontró InteractorPoseStore.");
        }
    }
    
    // private void Update()
    // {
    //     if (_grabs.Count == 0)
    //         return;
    //
    //     GrabState active = _grabs[_grabs.Count - 1];
    //
    //     // // Aquí problema: si no tienes Transform de la mano (solo posición), solo puedes usar posiciones
    //     // // Puedes tener almacenado el Pose inicial. Supongamos que anchorLocal fue almacenado, y evt del StartGrab incluía evt.Pose.position como world-space
    //     // // Para cálculo incremental, necesitas la posición actual del interactor con ese identifier.
    //     // // Supongamos que tienes una forma de obtener Transform o Pose actual del interactor con ese identifier:
    //     // Pose currentPose = GetPoseForIdentifier(active.identifier);
    //     // if (currentPose == null) return;
    //     if (!_poseStore.TryGetPose(active.identifier, out Pose currentPose))
    //     {
    //         // si no podemos conseguir la pose, no hacemos nada este frame
    //         return;
    //     }
    //
    //     Vector3 currentLocal = active.climbTransform.InverseTransformPoint(currentPose.position);
    //     Vector3 deltaLocal = currentLocal - active.anchorLocal;
    //     deltaLocal = Vector3.Scale(deltaLocal, active.allowedAxes);
    //     Vector3 deltaWorld = active.climbTransform.TransformVector(deltaLocal);
    //
    //     float dt = Time.deltaTime;
    //     if (dt <= 0f) dt = 0.0001f;
    //     Vector3 velocity = -deltaWorld / dt;
    //
    //     var pose = new Pose(velocity, Quaternion.identity);
    //
    //     LocomotionEvent ev = new LocomotionEvent(
    //         active.identifier,
    //         pose,
    //         LocomotionEvent.TranslationType.Velocity,
    //         LocomotionEvent.RotationType.None
    //     );
    //
    //     WhenLocomotionPerformed?.Invoke(ev);
    // }
    private void Update()
    {
        if (_grabs.Count == 0) return;

        GrabState active = _grabs[_grabs.Count - 1]; // O promedia si multi-hand

        if (!_poseStore.TryGetPose(active.identifier, out Pose currentPose))
        {
            Debug.LogWarning($"No pose for ID {active.identifier}");
            return;
        }

        Vector3 currentLocal = active.climbTransform.InverseTransformPoint(currentPose.position);
        Vector3 deltaLocal = currentLocal - active.anchorLocal;
        deltaLocal = Vector3.Scale(deltaLocal, active.allowedAxes);
        Vector3 deltaWorld = active.climbTransform.TransformVector(deltaLocal);

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 velocity = -deltaWorld / dt; // Solo Y positivo si quieres one-way climb: velocity.y = Mathf.Max(velocity.y, 0);

        var pose = new Pose(velocity, Quaternion.identity);

        LocomotionEvent ev = new LocomotionEvent(
            active.identifier,
            pose,
            LocomotionEvent.TranslationType.Velocity,
            LocomotionEvent.RotationType.None
        );

        WhenLocomotionPerformed?.Invoke(ev);
    }

    public void StartGrab(ClimbInteractable climb, int identifier, Vector3 anchorLocal, Vector3 allowedAxes)
    {
        GrabState state = new GrabState
        {
            identifier = identifier,
            climbTransform = climb.climbTransform,
            anchorLocal = anchorLocal,
            allowedAxes = allowedAxes
        };
        _grabs.Add(state);
    }

    public void FinishGrab(int identifier)
    {
        _grabs.RemoveAll(g => g.identifier == identifier);
    }

    // Método que necesitas implementar: dado el identificador, devolver la pose actual del interactor
    private Pose GetPoseForIdentifier(int identifier)
    {
        // Aquí depende de tu arquitectura:
        // Podrías tener un diccionario que mapea identificadores a Transformes de interactor
        // o usar alguna API del Interaction SDK que, dados los eventos de pointer, mantenga el tracking
        // XL placeholder:
        // return new Pose(somePosition, someRotation);
        return new Pose(Vector3.zero, Quaternion.identity);
    }
}