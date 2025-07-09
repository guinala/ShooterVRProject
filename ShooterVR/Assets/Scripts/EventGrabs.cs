using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Events;

public class EventGrabs : MonoBehaviour
{
    public HandGrabInteractable handGrabInteractable;
    public UnityEvent grabStart;
    public UnityEvent grabEnd;

    private void OnEnable()
    {
        if (handGrabInteractable != null)
        {
            handGrabInteractable.WhenSelectingInteractorAdded.Action += GrabEventStart;
            handGrabInteractable.WhenSelectingInteractorRemoved.Action += GrabEventEnd;
        }
    }

    private void OnDisable()
    {
        if (handGrabInteractable != null)
        {
            handGrabInteractable.WhenSelectingInteractorAdded.Action -= GrabEventStart;
            handGrabInteractable.WhenSelectingInteractorRemoved.Action -= GrabEventEnd;
        }
    }

    private void GrabEventStart(HandGrabInteractor interactor)
    {
        grabStart?.Invoke();
    }

    private void GrabEventEnd(HandGrabInteractor interactor)
    {
        grabEnd?.Invoke();
    }
}
