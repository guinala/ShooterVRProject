using System;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.Locomotion;   // Asegúrate de que el namespace coincida con tu versión
using Oculus.Interaction;             // Para los componentes de interacción / grab

public class ClimbInteractable : MonoBehaviour
{
    public Transform climbTransform;
    public float maxInteractionDistance = 0.2f;
    public bool filterByDistance = true;
    public Vector3 allowAxes = new Vector3(1, 1, 1);

    public ClimbLocomotionBroadcaster broadcaster;

    private Grabbable _grabbable;
    
    private InteractorPoses _poseStore;

    private void Awake()
    {
        if (climbTransform == null)
            climbTransform = this.transform;

        _grabbable = GetComponent<Grabbable>();
        if (_grabbable == null)
        {
            Debug.LogWarning($"ClimbInteractable_Meta: {gameObject.name} no tiene Grabbable.");
            // opcional: agregar, depende si quieres auto-añadir
            _grabbable = gameObject.AddComponent<Grabbable>();
        }
        _poseStore = FindObjectOfType<InteractorPoses>();
        if (_poseStore == null)
        {
            Debug.LogWarning("ClimbInteractable_Meta: no se encontró InteractorPoseStore en escena.");
        }
    }

    private void OnEnable()
    {
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised += OnPointerEvent;
        }
    }

    private void OnDisable()
    {
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised -= OnPointerEvent;
        }
    }

    private void OnPointerEvent(PointerEvent evt)
    {
        // registramos la pose siempre que haya un Move o Select (al menos Select da pose inicial)
        if (_poseStore != null)
        {
            _poseStore.UpdatePose(evt.Identifier, evt.Pose);
        }
        // Detectar Select / Unselect
        if (evt.Type == PointerEventType.Select)
        {
            HandleGrabStart(evt);
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            HandleGrabEnd(evt);
        }
    }

    // private void HandleGrabStart(PointerEvent evt)
    // {
    //     // evt.Identifier da el identificador del selector / interactor
    //     int identifier = evt.Identifier;
    //
    //     // Obtener transform de la mano/interactor
    //     // No tengo garantizado que PointerEvent lleve el transform del interactor.
    //     // Quizá necesites mapear el identificador a un interactor existente.
    //     // Ejemplo simplificado:
    //     Transform handTransform = evt.Pose.position != null ? /* buscar el interactor conforme al id */ null : null;
    //
    //     // Si no puedes obtener un Transform, puedes usar evt.Pose.position como proxy
    //     // O adaptar dependiendo de tu sistema
    //
    //     if (filterByDistance && climbTransform != null)
    //     {
    //         // tengo la posición del evento
    //         Vector3 eventPos = evt.Pose.position;
    //         float dist = Vector3.Distance(eventPos, climbTransform.position);
    //         if (dist > maxInteractionDistance)
    //         {
    //             return;
    //         }
    //     }
    //
    //     // Para anchor local: usar evt.Pose.position convertida a climbTransform espacio local
    //     Vector3 anchorLocal = climbTransform.InverseTransformPoint(evt.Pose.position);
    //
    //     broadcaster?.StartGrab(this, evt.Identifier, anchorLocal, allowAxes);
    // }
    // Antes, en ClimbInteractable_Meta
    // private void HandleGrabStart(PointerEvent evt)
    // {
    //     int id = evt.Identifier;
    //     Pose startPose = evt.Pose;
    //
    //     // opcional: validar distancia
    //
    //     Vector3 anchorLocal = climbTransform.InverseTransformPoint(startPose.position);
    //
    //     broadcaster?.StartGrab(this, id, anchorLocal, allowAxes);
    // }
    private void HandleGrabStart(PointerEvent evt)
    {
        int id = evt.Identifier;
        Pose startPose = evt.Pose;

        // opcional: validar distancia

        Vector3 anchorLocal = climbTransform.InverseTransformPoint(startPose.position);

        broadcaster?.StartGrab(this, id, anchorLocal, allowAxes);
    }


    private void HandleGrabEnd(PointerEvent evt)
    {
        broadcaster?.FinishGrab(evt.Identifier);
    }
}
