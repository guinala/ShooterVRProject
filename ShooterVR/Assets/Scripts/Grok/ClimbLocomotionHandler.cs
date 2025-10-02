using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Locomotion;

namespace Meta.Interaction.Locomotion.Climbing
{
    public class ClimbLocomotionHandler : MonoBehaviour
    {
        [SerializeField]
        private List<FirstPersonLocomotor> providersToDisable = new List<FirstPersonLocomotor>();

        [SerializeField]
        private bool enableGravityOnEnd = true;

        [SerializeField]
        public ClimbSettings climbSettings = new ClimbSettings();

        [SerializeField]
        private FirstPersonLocomotor firstPersonLocomotor;

        private List<HandGrabInteractor> grabbingInteractors = new List<HandGrabInteractor>();
        private List<ClimbHandle> grabbedHandles = new List<ClimbHandle>();

        private Vector3 interactorAnchorWorldPos;
        private Vector3 interactorAnchorClimbSpacePos;

        private List<FirstPersonLocomotor> enabledProvidersToDisable = new List<FirstPersonLocomotor>();
        private bool isClimbing;

        public event System.Action<ClimbLocomotionHandler> climbAnchorUpdated;
        // por interactor, almacenamos su posición anterior en espacio "climb"
        private Dictionary<HandGrabInteractor, Vector3> previousInteractorClimbPos = new Dictionary<HandGrabInteractor, Vector3>();
        // Fuente consistente de posición por interactor (Transform que realmente se mueve con la mano)
        private Dictionary<HandGrabInteractor, Transform> interactorPositionSource = new Dictionary<HandGrabInteractor, Transform>();


        // Campos (colócalos en la clase)
        [Header("OVR Anchors (optional)")]
        [SerializeField]
        private Transform leftHandAnchorInspector;
        [SerializeField]
        private Transform rightHandAnchorInspector;

        private Transform leftHandAnchor;
        private Transform rightHandAnchor;

        // Llamar en Awake o Start
        private void TryResolveHandAnchors()
        {
            leftHandAnchor = leftHandAnchorInspector;
            rightHandAnchor = rightHandAnchorInspector;

            if (leftHandAnchor != null && rightHandAnchor != null) return;

            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null)
            {
                var t = rig.transform.Find("TrackingSpace");
                if (t != null)
                {
                    if (leftHandAnchor == null) leftHandAnchor = t.Find("LeftHandAnchor");
                    if (rightHandAnchor == null) rightHandAnchor = t.Find("RightHandAnchor");
                }
            }

            // Si aún no se encuentran, buscar por nombre en la escena
            if (leftHandAnchor == null)
            {
                var go = GameObject.Find("LeftHandAnchor");
                if (go != null) leftHandAnchor = go.transform;
            }
            if (rightHandAnchor == null)
            {
                var go = GameObject.Find("RightHandAnchor");
                if (go != null) rightHandAnchor = go.transform;
            }
        }


        // Función que usaremos para leer la posición "real" de la mano
        private Vector3 GetInteractorWorldPos(HandGrabInteractor interactor)
        {
            if (interactor == null) return Vector3.zero;

            // 1) intento directo (si no es 0 lo uso)
            Vector3 pos = interactor.transform.position;
            if (pos.sqrMagnitude > 1e-6f) return pos;

            // 2) intentar obtener un AttachTransform si la clase lo exporta (reflect)
            var prop = interactor.GetType().GetProperty("AttachTransform");
            if (prop != null)
            {
                var at = prop.GetValue(interactor) as Transform;
                if (at != null && at.position.sqrMagnitude > 1e-6f) return at.position;
            }

            // 3) fallback a los anchors Left/Right (intenta resolver si no lo hicimos)
            if (leftHandAnchor == null || rightHandAnchor == null) TryResolveHandAnchors();

            string nameLower = interactor.name != null ? interactor.name.ToLower() : "";
            if (nameLower.Contains("left") && leftHandAnchor != null) return leftHandAnchor.position;
            if (nameLower.Contains("right") && rightHandAnchor != null) return rightHandAnchor.position;

            // 4) si no sabemos cuál, usar primer hijo que exista (parche temporal)
            if (interactor.transform.childCount > 0)
            {
                var child = interactor.transform.GetChild(0);
                if (child.position.sqrMagnitude > 1e-6f) return child.position;
            }
            
            if (pos.sqrMagnitude < 1e-6f)
            {
                Debug.Log($"[Climb] GetInteractorWorldPos: interactor {interactor.name} root at (0,0,0). Falling back.");
            }

            // 5) último recurso: devolver el pos (aunque sea 0)
            return pos;
        }


        // private void Awake()
        // {
        //     if (firstPersonLocomotor == null)
        //     {
        //         firstPersonLocomotor = FindObjectOfType<FirstPersonLocomotor>();
        //     }
        // }
        
        private void Awake()
        {
            if (firstPersonLocomotor == null)
            {
                firstPersonLocomotor = FindObjectOfType<FirstPersonLocomotor>();
            }
        }

        private void Update()
        {
            if (isClimbing && grabbingInteractors.Count > 0)
            {
                var lastIndex = grabbingInteractors.Count - 1;
                var interactor = grabbingInteractors[lastIndex];
                var handle = grabbedHandles[lastIndex];

                StepClimbMovement(handle, interactor);
            }
            else if (isClimbing)
            {
                FinishClimbing();
            }
        }

        public void StartClimbGrab(ClimbHandle handle, HandGrabInteractor interactor)
        {
            Debug.Log("StartClimbGrab: " + interactor?.name);
            grabbingInteractors.Add(interactor);
            grabbedHandles.Add(handle);
        
            // Determinar y guardar la fuente de posición para este interactor
            Transform source = null;
        
            // 1) intentar propiedad AttachTransform por reflection
            var prop = interactor.GetType().GetProperty("AttachTransform");
            if (prop != null)
            {
                var at = prop.GetValue(interactor) as Transform;
                if (at != null && at.position.sqrMagnitude > 1e-6f)
                {
                    source = at;
                    Debug.Log($"[Climb] StartClimbGrab: using AttachTransform for {interactor.name}");
                }
            }
        
            // 2) si no hay attach, busca un hijo que cambie de posición (heurística: el primer hijo con posición no-zero)
            if (source == null && interactor.transform.childCount > 0)
            {
                for (int i = 0; i < interactor.transform.childCount; i++)
                {
                    var c = interactor.transform.GetChild(i);
                    if (c.position.sqrMagnitude > 1e-6f)
                    {
                        source = c;
                        Debug.Log($"[Climb] StartClimbGrab: using child '{c.name}' as source for {interactor.name}");
                        break;
                    }
                }
            }
        
            // 3) fallback: usar los anchors izquierdo/derecho si los tenemos
            if (source == null)
            {
                if (leftHandAnchor == null || rightHandAnchor == null) TryResolveHandAnchors();
                string nameLower = interactor.name != null ? interactor.name.ToLower() : "";
                if (nameLower.Contains("left") && leftHandAnchor != null) { source = leftHandAnchor; Debug.Log("[Climb] using leftHandAnchor fallback"); }
                else if (nameLower.Contains("right") && rightHandAnchor != null) { source = rightHandAnchor; Debug.Log("[Climb] using rightHandAnchor fallback"); }
            }
        
            // 4) último recurso: usar el transform del interactor (aunque sea 0)
            if (source == null) { source = interactor.transform; Debug.LogWarning($"[Climb] StartClimbGrab: using interactor.transform for {interactor.name} (may be static at 0,0,0)"); }
        
            // guardar la fuente
            interactorPositionSource[interactor] = source;
        
            // ahora inicializa el anchor y prev usando esa fuente (UpdateClimbAnchor usa la fuente si existe)
            UpdateClimbAnchor(handle, interactor);
        
            isClimbing = true;
            Physics.gravity = Vector3.zero;
        
            foreach (var provider in providersToDisable)
            {
                if (provider != null && provider.enabled)
                {
                    provider.enabled = false;
                    enabledProvidersToDisable.Add(provider);
                }
            }
        }



        public void FinishClimbGrab(HandGrabInteractor interactor)
        {
            var index = grabbingInteractors.IndexOf(interactor);
            if (index < 0) return;

            if (index == grabbingInteractors.Count - 1 && index > 0)
            {
                UpdateClimbAnchor(grabbedHandles[index - 1], grabbingInteractors[index - 1]);
            }

            grabbingInteractors.RemoveAt(index);
            grabbedHandles.RemoveAt(index);

            // limpia el registro previo
            if (previousInteractorClimbPos.ContainsKey(interactor))
            {
                previousInteractorClimbPos.Remove(interactor);
            }
            if (interactorPositionSource.ContainsKey(interactor)) interactorPositionSource.Remove(interactor);
        }


        private void UpdateClimbAnchor(ClimbHandle handle, HandGrabInteractor interactor)
        {
            var climbTrans = handle.ClimbTransform;

            // obtener la fuente asignada (si no existe, intenta GetInteractorWorldPos como backup)
            Transform source;
            if (!interactorPositionSource.TryGetValue(interactor, out source) || source == null)
            {
                // intenta resolverla rápido (compatibilidad)
                Vector3 fallbackWorld = GetInteractorWorldPos(interactor);
                if (fallbackWorld.sqrMagnitude > 1e-6f)
                {
                    interactorAnchorWorldPos = fallbackWorld;
                }
                else
                {
                    TryResolveHandAnchors();
                    // fallback a nearest anchor si no hay source
                    if (leftHandAnchor != null && rightHandAnchor != null)
                    {
                        Vector3 handlePos = climbTrans.position;
                        float dl = Vector3.SqrMagnitude(leftHandAnchor.position - handlePos);
                        float dr = Vector3.SqrMagnitude(rightHandAnchor.position - handlePos);
                        interactorAnchorWorldPos = dl <= dr ? leftHandAnchor.position : rightHandAnchor.position;
                    }
                    else
                    {
                        interactorAnchorWorldPos = fallbackWorld; // (probablemente 0)
                    }
                }
            }
            else
            {
                interactorAnchorWorldPos = source.position;
            }

            // calculamos la posicion en espacio climb y la guardamos como 'prev' para evitar teletransportes
            interactorAnchorClimbSpacePos = climbTrans.InverseTransformPoint(interactorAnchorWorldPos);
            previousInteractorClimbPos[interactor] = interactorAnchorClimbSpacePos;

            climbAnchorUpdated?.Invoke(this);

            Debug.Log($"[Climb] UpdateClimbAnchor: interactor={interactor?.name}, anchorWorld={interactorAnchorWorldPos}, source={(source!=null?source.name:"null")}, interactorClimbPos={interactorAnchorClimbSpacePos}");
        }




        private void StepClimbMovement(ClimbHandle handle, HandGrabInteractor interactor)
        {
            var settings = handle.ActiveSettings;
            var climbTrans = handle.ClimbTransform;

            // obtener la posicion actual de la mano en espacio climb
            //var interactorClimbPos = climbTrans.InverseTransformPoint(GetInteractorWorldPos(interactor));
            // obtener la posicion actual de la mano en espacio climb usando la fuente registrada
            Transform source = null;
            if (!interactorPositionSource.TryGetValue(interactor, out source) || source == null)
            {
                source = interactor.transform; // último recurso
            }
            var interactorClimbPos = handle.ClimbTransform.InverseTransformPoint(source.position);

            // obtener prev (si no existe, inicializamos y salimos este frame)
            if (!previousInteractorClimbPos.TryGetValue(interactor, out Vector3 prevInteractorClimbPos))
            {
                previousInteractorClimbPos[interactor] = interactorClimbPos;
                return;
            }

            // delta en espacio climb: cuánto se ha movido la mano desde el frame anterior
            Vector3 deltaClimbSpace = prevInteractorClimbPos - interactorClimbPos;

            // aplica restricciones de ejes
            if (!settings.allowFreeXMovement) deltaClimbSpace.x = 0f;
            if (!settings.allowFreeYMovement) deltaClimbSpace.y = 0f;
            if (!settings.allowFreeZMovement) deltaClimbSpace.z = 0f;

            // opcional: invierte si quieres que mover la mano hacia arriba haga subir (ajusta según tu UX)
            // deltaClimbSpace = -deltaClimbSpace;

            // transformar a world
            Vector3 movement = climbTrans.TransformVector(deltaClimbSpace);

            // seguridad: evita saltos enormes
            float maxStep = 1.0f; // metros por frame máximo — ajusta entre 0.2 y 1.0 según sensación
            if (movement.magnitude > maxStep)
            {
                Debug.LogWarning($"[Climb] movement clamped from {movement.magnitude} to {maxStep}");
                movement = Vector3.ClampMagnitude(movement, maxStep);
            }

            // seguridad: NaN / Inf
            if (float.IsNaN(movement.x) || float.IsInfinity(movement.x))
            {
                Debug.LogError("[Climb] movement invalid, ignored");
            }
            else
            {
                Debug.Log($"[Climb] anchorWorld={interactorAnchorWorldPos}, interactorClimbPos={interactorClimbPos}, movement={movement}, mag={movement.magnitude}");
                var locomotionEvent = new LocomotionEvent(0, new Pose(movement, Quaternion.identity), LocomotionEvent.TranslationType.Relative, LocomotionEvent.RotationType.None);
                firstPersonLocomotor.HandleLocomotionEvent(locomotionEvent);
            }

            // actualizar prev para el siguiente frame
            previousInteractorClimbPos[interactor] = interactorClimbPos;
        }

        
        private void FinishClimbing()
        {
            var currentHandle = grabbedHandles.Count > 0 ? grabbedHandles[grabbedHandles.Count - 1] : null;

            isClimbing = false;
            grabbingInteractors.Clear();
            grabbedHandles.Clear();

            if (enableGravityOnEnd)
            {
                Physics.gravity = new Vector3(0, -9.81f, 0);
            }

            foreach (var provider in enabledProvidersToDisable)
            {
                if (provider != null)
                {
                    provider.enabled = true;
                }
            }
            enabledProvidersToDisable.Clear();

            // Teletransporte asistido
            if (currentHandle != null && currentHandle.ClimbAssistanceDestination != null)
            {
                Pose targetPose = new Pose(currentHandle.ClimbAssistanceDestination.position, currentHandle.ClimbAssistanceDestination.rotation);
                var locomotionEvent = new LocomotionEvent(0, targetPose, LocomotionEvent.TranslationType.Absolute, LocomotionEvent.RotationType.Absolute);
                firstPersonLocomotor.HandleLocomotionEvent(locomotionEvent);
            }
            

        }
    }
}