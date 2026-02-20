using System.Reflection;

using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A static utility class providing extension methods for <see cref="UnityEvent"/> 
/// types that register listeners as persistent (serialized) entries, making them visible in the 
/// Unity Inspector. In non-editor builds, it falls back to standard runtime 
/// <seealso cref="UnityEvent.AddListener(UnityAction)"/> /
/// <seealso cref="UnityEvent.RemoveListener(UnityAction)"/>.
/// </summary>
public static class UnityEventInInspector {
#if UNITY_EDITOR
    private static void AddListener(UnityEventBase unityEvent, Object target, MethodInfo method) {
        var type = unityEvent.GetType();
        var registerPersistentListener = type.GetMethod("RegisterPersistentListener",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(int), typeof(object), typeof(MethodInfo) }, null);
        var count = unityEvent.GetPersistentEventCount();
        var addListener = type.GetMethod("AddPersistentListener",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            System.Array.Empty<System.Type>(), null);
        addListener.Invoke(unityEvent, null);
        registerPersistentListener.Invoke(unityEvent, new object[] { count, target, method });

        UnityEditor.EditorUtility.SetDirty(target);

    }
    private static void RemoveListener(UnityEventBase unityEvent, Object target, MethodInfo method) {
        var type = unityEvent.GetType();
        var removePersistentListener = type.GetMethod("RemovePersistentListener",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(Object), typeof(MethodInfo) }, null);
        removePersistentListener.Invoke(unityEvent, new object[] { target, method });
    }
#endif
    /// <summary>
    /// Registers a persistent listener on a <see cref="UnityEvent"/> so it appears in the Inspector. 
    /// In a non-editor build, delegates to the standard <seealso cref="UnityEvent.AddListener(UnityAction)"/>.
    /// </summary>
    /// <param name="unityEvent">The target <seealso cref="UnityEvent"/> instance (extended via this)</param>
    /// <param name="method">The delegate (<seealso cref="UnityAction"/> variant) pointing to the callback</param>
    /// <param name="target">The <seealso cref="UnityEngine.Object"/> that owns the method; used to resolve the MethodInfo 
    /// via reflection and to mark the asset dirty in the editor.</param>
    public static void AddListener(this UnityEvent unityEvent, UnityAction method, Object target) {
#if !UNITY_EDITOR
        unityEvent.AddListener(method);
#else
        var methodName = method.Method.Name;
        var targetMethod = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AddListener(unityEvent, target, targetMethod);
#endif
    }

    /// <summary>
    /// Registers a persistent listener with one parameter of type T.
    /// </summary>
    /// <param name="unityEvent">The target <seealso cref="UnityEvent"/> instance (extended via this)</param>
    /// <param name="method">The delegate (<seealso cref="UnityAction"/> variant) pointing to the callback</param>
    /// <param name="target">The <seealso cref="UnityEngine.Object"/> that owns the method; used to resolve the MethodInfo 
    /// via reflection and to mark the asset dirty in the editor.</param>
    public static void AddListener<T>(this UnityEvent<T> unityEvent, UnityAction<T> method, Object target) {
#if !UNITY_EDITOR
        unityEvent.AddListener(method);
#else
        var methodName = method.Method.Name;
        var targetMethod = target.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(T) }, null);
        AddListener(unityEvent, target, targetMethod);
#endif

    }

    /// <summary>
    /// Registers a persistent listener with two parameters (T1, T2).
    /// </summary>
    /// <param name="unityEvent">The target <seealso cref="UnityEvent"/> instance (extended via this)</param>
    /// <param name="method">The delegate (<seealso cref="UnityAction"/> variant) pointing to the callback</param>
    /// <param name="target">The <seealso cref="UnityEngine.Object"/> that owns the method; used to resolve the MethodInfo 
    /// via reflection and to mark the asset dirty in the editor.</param>
    public static void AddListener<T1, T2>(this UnityEvent<T1, T2> unityEvent, UnityAction<T1, T2> method, Object target) {
#if !UNITY_EDITOR
        unityEvent.AddListener(method);
#else
        var methodName = method.Method.Name;
        var targetMethod = target.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(T1), typeof(T2) }, null);
        AddListener(unityEvent, target, targetMethod);
#endif

    }

    /// <summary>
    /// Registers a persistent listener with three parameters (T1, T2, T3).
    /// </summary>
    /// <param name="unityEvent">The target <seealso cref="UnityEvent"/> instance (extended via this)</param>
    /// <param name="method">The delegate (<seealso cref="UnityAction"/> variant) pointing to the callback</param>
    /// <param name="target">The <seealso cref="UnityEngine.Object"/> that owns the method; used to resolve the MethodInfo 
    /// via reflection and to mark the asset dirty in the editor.</param>
    public static void AddListener<T1, T2, T3>(this UnityEvent<T1, T2, T3> unityEvent, UnityAction<T1, T2, T3> method, Object target) {
#if !UNITY_EDITOR
        unityEvent.AddListener(method);
#else
        var methodName = method.Method.Name;
        var targetMethod = target.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(T1), typeof(T2), typeof(T3) }, null);
        AddListener(unityEvent, target, targetMethod);
#endif

    }

    /// <summary>
    /// Removes a persistent listener from a <see cref="UnityEvent"/>. In a non-editor build, 
    /// delegates to the standard <seealso cref="UnityEvent.RemoveListener(UnityAction)"/>.
    /// </summary>
    /// <param name="unityEvent">The target <seealso cref="UnityEvent"/> instance (extended via this)</param>
    /// <param name="method">The delegate (<seealso cref="UnityAction"/> variant) pointing to the callback</param>
    /// <param name="target">The <seealso cref="UnityEngine.Object"/> that owns the method; used to resolve the MethodInfo 
    /// via reflection and to mark the asset dirty in the editor.</param>
    public static void RemoveListener(this UnityEvent unityEvent, UnityAction method, Object target) {
#if !UNITY_EDITOR
        unityEvent.RemoveListener(method);
#else
        var methodName = method.Method.Name;
        var targetMethod = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        RemoveListener(unityEvent, target, targetMethod);
#endif
    }

    /// <summary>
    /// Removes a persistent listener with one parameter of type T.
    /// </summary>
    /// <param name="unityEvent">The target <seealso cref="UnityEvent"/> instance (extended via this)</param>
    /// <param name="method">The delegate (<seealso cref="UnityAction"/> variant) pointing to the callback</param>
    /// <param name="target">The <seealso cref="UnityEngine.Object"/> that owns the method; used to resolve the MethodInfo 
    /// via reflection and to mark the asset dirty in the editor.</param>
    public static void RemoveListener<T>(this UnityEvent<T> unityEvent, UnityAction<T> method, Object target) {
#if !UNITY_EDITOR
        unityEvent.RemoveListener(method);
#else
        var methodName = method.Method.Name;
        var targetMethod = target.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(T) }, null);
        RemoveListener(unityEvent, target, targetMethod);
#endif
    }

    /// <summary>
    /// Removes a persistent listener with two parameters (T1, T2).
    /// </summary>
    /// <param name="unityEvent">The target <seealso cref="UnityEvent"/> instance (extended via this)</param>
    /// <param name="method">The delegate (<seealso cref="UnityAction"/> variant) pointing to the callback</param>
    /// <param name="target">The <seealso cref="UnityEngine.Object"/> that owns the method; used to resolve the MethodInfo 
    /// via reflection and to mark the asset dirty in the editor.</param>
    public static void RemoveListener<T1, T2>(this UnityEvent<T1, T2> unityEvent, UnityAction<T1, T2> method, Object target) {
#if !UNITY_EDITOR
        unityEvent.RemoveListener(method);
#else
        var methodName = method.Method.Name;
        var targetMethod = target.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(T1), typeof(T2) }, null);
        RemoveListener(unityEvent, target, targetMethod);
#endif
    }

    /// <summary>
    /// Removes a persistent listener with three parameters (T1, T2, T3).
    /// </summary>
    /// <param name="unityEvent">The target <seealso cref="UnityEvent"/> instance (extended via this)</param>
    /// <param name="method">The delegate (<seealso cref="UnityAction"/> variant) pointing to the callback</param>
    /// <param name="target">The <seealso cref="UnityEngine.Object"/> that owns the method; used to resolve the MethodInfo 
    /// via reflection and to mark the asset dirty in the editor.</param>
    public static void RemoveListener<T1, T2, T3>(this UnityEvent<T1, T2, T3> unityEvent, UnityAction<T1, T2, T3> method, Object target) {
#if !UNITY_EDITOR
        unityEvent.RemoveListener(method);
#else
        var methodName = method.Method.Name;
        var targetMethod = target.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(T1), typeof(T2), typeof(T3) }, null);
        RemoveListener(unityEvent, target, targetMethod);
#endif
    }
}
