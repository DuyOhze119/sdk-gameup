using System;
using System.Collections;
using Firebase;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseUtils : MonoSingleton<FirebaseUtils>
{
    private DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
    private bool isInitialized = false;
    private bool isInitializing = false;

    public bool IsInitialized => isInitialized;
    public DependencyStatus Status => dependencyStatus;

    public event Action OnFirebaseInitialized;
    public event Action<string> OnFirebaseInitializationFailed;

    protected override void Awake()
    {
        base.Awake();
        FirebaseInit();
    }

    /// <summary>
    /// Khởi tạo Firebase và kiểm tra dependencies
    /// </summary>
    public void FirebaseInit()
    {
        if (isInitialized)
        {
            Debug.Log("Firebase đã được khởi tạo trước đó.");
            OnFirebaseInitialized?.Invoke();
            return;
        }

        if (isInitializing)
        {
            Debug.LogWarning("Firebase đang được khởi tạo, vui lòng đợi...");
            return;
        }

        isInitializing = true;
        Debug.Log("Bắt đầu kiểm tra Firebase dependencies...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            isInitializing = false;
            dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase đã sẵn sàng
                FirebaseApp app = FirebaseApp.DefaultInstance;
                Debug.Log($"Firebase khởi tạo thành công! App Name: {app.Name}");
                
                isInitialized = true;
                OnFirebaseInitialized?.Invoke();
            }
            else
            {
                // Firebase không khả dụng
                string errorMessage = $"Firebase không thể khởi tạo. Dependency Status: {dependencyStatus}";
                Debug.LogError(errorMessage);
                
                isInitialized = false;
                OnFirebaseInitializationFailed?.Invoke(errorMessage);
            }
        });
    }

    /// <summary>
    /// Khởi tạo lại Firebase (dùng khi cần reset)
    /// </summary>
    public void ReinitializeFirebase()
    {
        if (isInitializing)
        {
            Debug.LogWarning("Firebase đang được khởi tạo, không thể khởi tạo lại.");
            return;
        }

        isInitialized = false;
        dependencyStatus = DependencyStatus.UnavailableOther;
        FirebaseInit();
    }

    /// <summary>
    /// Kiểm tra xem Firebase có sẵn sàng sử dụng không
    /// </summary>
    public bool IsFirebaseReady()
    {
        return isInitialized && dependencyStatus == DependencyStatus.Available;
    }
}
