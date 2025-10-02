using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using UnityEngine.Events;

public class GrabTransformerEventSender : GrabFreeTransformer, ITransformer
{
    public UnityEvent onObjectGrabbed;
    public UnityEvent onObjectMoved;
    public UnityEvent onObjectReleased;
    public static event System.Action<IGrabbable> OnObjectGrabbed;  // Pasamos IGrabbable para identificar el objeto
    public static event System.Action<IGrabbable> OnObjectReleased;
    public static event System.Action<IGrabbable> OnObjectMoved;
    public bool useActions;
    private IGrabbable grabbable;

    public new void Initialize(IGrabbable grabbable)
    {
        base.Initialize(grabbable);
        this.grabbable = grabbable;
    }

    public new void BeginTransform()
    {
        base.BeginTransform();
        if (useActions)
            OnObjectGrabbed?.Invoke(grabbable);
        else
            onObjectGrabbed?.Invoke();
    }

    public new void UpdateTransform()
    {
        base.UpdateTransform();
        if (useActions)
            OnObjectMoved?.Invoke(grabbable);
        else
            onObjectMoved?.Invoke();
    }

    public new void EndTransform()
    {
        // base.EndTransform();  // Descomenta si extiendes de otro transformer en el futuro
        if (useActions)
            OnObjectReleased?.Invoke(grabbable);
        else
            onObjectReleased?.Invoke();
    }
}