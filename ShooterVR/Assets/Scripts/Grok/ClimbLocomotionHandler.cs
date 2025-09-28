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
            grabbingInteractors.Add(interactor);
            grabbedHandles.Add(handle);
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
        }

        private void UpdateClimbAnchor(ClimbHandle handle, HandGrabInteractor interactor)
        {
            var climbTrans = handle.ClimbTransform;
            interactorAnchorWorldPos = interactor.transform.position;
            interactorAnchorClimbSpacePos = climbTrans.InverseTransformPoint(interactorAnchorWorldPos);
            climbAnchorUpdated?.Invoke(this);
        }

        private void StepClimbMovement(ClimbHandle handle, HandGrabInteractor interactor)
        {
            var settings = handle.ActiveSettings;
            var interactorPos = interactor.transform.position;
            Vector3 movement;

            if (settings.allowFreeXMovement && settings.allowFreeYMovement && settings.allowFreeZMovement)
            {
                movement = interactorAnchorWorldPos - interactorPos;
            }
            else
            {
                var climbTrans = handle.ClimbTransform;
                var interactorClimbPos = climbTrans.InverseTransformPoint(interactorPos);
                var movementClimbSpace = interactorAnchorClimbSpacePos - interactorClimbPos;

                if (!settings.allowFreeXMovement) movementClimbSpace.x = 0f;
                if (!settings.allowFreeYMovement) movementClimbSpace.y = 0f;
                if (!settings.allowFreeZMovement) movementClimbSpace.z = 0f;

                movement = climbTrans.TransformVector(movementClimbSpace);
            }

            var locomotionEvent = new LocomotionEvent(0, new Pose(movement, Quaternion.identity), LocomotionEvent.TranslationType.Relative, LocomotionEvent.RotationType.None);
            firstPersonLocomotor.HandleLocomotionEvent(locomotionEvent);
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