using System.Runtime.CompilerServices;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;

namespace Meta.Interaction.Locomotion.Climbing
{
    [RequireComponent(typeof(HandGrabInteractable))]
    public class ClimbHandle : MonoBehaviour
    {
        [SerializeField]
        private ClimbLocomotionHandler climbHandler;

        [SerializeField]
        private Transform climbTransform;

        [SerializeField]
        private bool filterByDistance = true;

        [SerializeField]
        private float maxInteractionDistance = 0.1f;

        [SerializeField]
        private Transform climbAssistanceDestination;

        [SerializeField]
        private ClimbSettings climbSettingsOverride;

        private HandGrabInteractable interactable;
        private HandGrabInteractor currentInteractor;

        private void Awake()
        {
            interactable = GetComponent<HandGrabInteractable>();
            if (climbHandler == null)
            {
                climbHandler = FindObjectOfType<ClimbLocomotionHandler>();
            }
            if (climbTransform == null)
            {
                climbTransform = transform;
            }
        }

        private void OnEnable()
        {
            interactable.WhenPointerEventRaised += HandlePointerEvent;
        }

        private void OnDisable()
        {
            interactable.WhenPointerEventRaised -= HandlePointerEvent;
        }

        private void HandlePointerEvent(PointerEvent evt)
        {
            Debug.Log("SE VIENE");
            if (evt.Type == PointerEventType.Select)
            {
                Debug.Log("Se viene el select");
                currentInteractor = GetInteractorFromEvent(evt);
                if (currentInteractor == null)
                {
                    Debug.Log("Me voy a ir de vacaciones");
                    return;
                }

                if (filterByDistance && Vector3.Distance(currentInteractor.transform.position, transform.position) > maxInteractionDistance)
                {
                    Debug.Log("Silson");
                    return;
                }
                Debug.Log("Hora de escalar");
                climbHandler.StartClimbGrab(this, currentInteractor);
            }
            else if (evt.Type == PointerEventType.Unselect)
            {
                if (currentInteractor != null)
                {
                    climbHandler.FinishClimbGrab(currentInteractor);
                    currentInteractor = null;
                }
            }
            // Opcional: PointerEventType.Move para update durante movimiento
        }

        private HandGrabInteractor GetInteractorFromEvent(PointerEvent evt)
        {
            Debug.Log("La data es: " + evt.Data);
            if (evt.Data is HandGrabInteractor handData)
            {
                Debug.Log("Entro aqui");
                var interactors = FindObjectsOfType<HandGrabInteractor>();
                foreach (var interactor in interactors)
                {
                    Debug.Log("Esto es: " + interactor);
                    if (interactor == handData)
                    {
                        Debug.Log("CINE");
                        return interactor;
                    }
                }
            }
            else
            {
                Debug.Log("Ni siuquiera entro aqui");
            }
            

            // Si usas controllers, agrega lógica similar aquí (evt.Data is IController, match interactor.Controller if exists)
            return null;
        }

        public Transform ClimbTransform => climbTransform;
        public ClimbSettings ActiveSettings => climbSettingsOverride ?? climbHandler.climbSettings;
        public Transform ClimbAssistanceDestination => climbAssistanceDestination;
    }
}